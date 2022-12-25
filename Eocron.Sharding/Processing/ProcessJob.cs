using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Eocron.Sharding.Configuration;
using Eocron.Sharding.Jobs;
using Microsoft.Extensions.Logging;

namespace Eocron.Sharding.Processing
{
    public sealed class ProcessJob<TInput, TOutput, TError> : 
        IShard, 
        IShardOutputProvider<TOutput, TError>, 
        IShardInputManager<TInput>, 
        IProcessDiagnosticInfoProvider, 
        IJob
    {
        private readonly ProcessShardOptions _options;
        private readonly TimeSpan _statusCheckInterval;
        private readonly IStreamReaderDeserializer<TOutput> _outputDeserializer;
        private readonly IStreamReaderDeserializer<TError> _errorDeserializer;
        private readonly IStreamWriterSerializer<TInput> _inputSerializer;
        private readonly ILogger _logger;
        private readonly IProcessStateProvider _stateProvider;
        private readonly IChildProcessWatcher _watcher;
        private readonly Channel<ShardMessage<TOutput>> _outputs;
        private readonly Channel<ShardMessage<TError>> _errors;
        private readonly SemaphoreSlim _publishSemaphore;
        private readonly string _shardId;
        private Process _currentProcess;
        private bool _disposed;

        public string Id => _shardId;
        public ChannelReader<ShardMessage<TOutput>> Outputs => _outputs.Reader;
        public ChannelReader<ShardMessage<TError>> Errors => _errors.Reader;
        public ProcessJob(
            ProcessShardOptions options,
            IStreamReaderDeserializer<TOutput> outputDeserializer,
            IStreamReaderDeserializer<TError> errorDeserializer,
            IStreamWriterSerializer<TInput> inputSerializer,
            ILogger logger,
            IProcessStateProvider stateProvider = null,
            IChildProcessWatcher watcher = null,
            string id = null)
        {
            _options = options;
            _outputDeserializer = outputDeserializer;
            _errorDeserializer = errorDeserializer;
            _inputSerializer = inputSerializer;
            _logger = logger;
            _stateProvider = stateProvider;
            _watcher = watcher;
            _outputs = Channel.CreateBounded<ShardMessage<TOutput>>(_options.OutputOptions ?? ProcessShardOptions.DefaultOutputOptions);
            _errors = Channel.CreateBounded<ShardMessage<TError>>(_options.ErrorOptions ?? ProcessShardOptions.DefaultErrorOptions);
            _statusCheckInterval = _options.ProcessStatusCheckInterval ?? ProcessShardOptions.DefaultProcessStatusCheckInterval;
            _publishSemaphore = new SemaphoreSlim(1);
            _shardId = id ?? $"process_shard_{Guid.NewGuid():N}";
        }

        public bool TryGetProcessDiagnosticInfo(out ProcessDiagnosticInfo info)
        {
            info = null;
            var current = _currentProcess;
            if (current == null)
                return false;
            
            info = new ProcessDiagnosticInfo
            {
                PrivateMemorySize64 = current.PrivateMemorySize64,
                TotalProcessorTime = current.TotalProcessorTime,
                WorkingSet64 = current.WorkingSet64,
                ModuleName = current.MainModule.ModuleName,
            };
            return true;
        }

        public bool IsReady()
        {
            var process = _currentProcess;
            return process != null
                   && !process.HasExited
                   && _publishSemaphore.CurrentCount > 0
                   && IsReadyForPublish(process);
        }

        public async Task PublishAsync(IEnumerable<TInput> messages, CancellationToken ct)
        {
            if (messages == null)
                throw new ArgumentNullException(nameof(messages));
            await _publishSemaphore.WaitAsync(ct).ConfigureAwait(false);
            try
            {
                var process = await GetRunningProcessAsync(ct).ConfigureAwait(false);
                using var logScope = BeginProcessLoggingScope(process);
                await _inputSerializer.SerializeTo(process.StandardInput, messages, ct).ConfigureAwait(false);
                if (process.HasExited && process.ExitCode != 0)
                {
                    throw CreatePublishedWithErrorException(process);
                }
            }
            finally
            {
                _publishSemaphore.Release();
            }
        }

        public async Task RunAsync(CancellationToken stopToken)
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(stopToken);
            using var process = Process.Start(_options.StartInfo);
            using var register = cts.Token.Register(() => OnCancellation(process));
            using var logScope = BeginProcessLoggingScope(process);

            if (_watcher != null)
            {
                await _watcher.ChildrenToWatch.Writer.WriteAsync(process.Id, cts.Token).ConfigureAwait(false);
            }

            _logger.LogInformation("Process {process_id} shard started", process.Id);
            await WaitUntilReady(process, cts.Token).ConfigureAwait(false);
            _logger.LogInformation("Process {process_id} shard ready for publish", process.Id);
            var ioTasks = new[]
            {
                ProcessStreamReader(process.StandardOutput, _outputDeserializer, _outputs, process, cts.Token),
                ProcessStreamReader(process.StandardError, _errorDeserializer, _errors, process, cts.Token)
            };
            _currentProcess = process;
            await WaitUntilExit(process);
            cts.Cancel();
            await Task.WhenAll(ioTasks).ConfigureAwait(false);

            if (stopToken.IsCancellationRequested)
            {
                if (process.ExitCode == 0)
                {
                    _logger.LogInformation("Process {process_id} shard gracefully cancelled", process.Id);
                }
                else
                {
                    _logger.LogWarning("Process {process_id} shard cancelled with exit code {exit_code}",
                        process.Id, process.ExitCode);
                }
            }
            else
            {
                if (process.ExitCode == 0)
                {
                    _logger.LogWarning("Process {process_id} shard suddenly stopped without error", process.Id);
                }
                else
                {
                    _logger.LogError("Process {process_id} shard suddenly stopped with exit code {exit_code}",
                        process.Id, process.ExitCode);
                    throw CreateProcessExitCodeException(process);
                }
            }
        }

        private IDisposable BeginProcessLoggingScope(Process process)
        {
            return _logger.BeginScope(new Dictionary<string, string>()
            {
                { "processId", process.Id.ToString() },
                { "shardId", _shardId }
            });
        }

        private bool IsReadyForPublish(Process process)
        {
            return _stateProvider?.IsReady(process) ?? true;
        }

        private async Task<Process> GetRunningProcessAsync(CancellationToken ct)
        {
            while (true)
            {
                var process = _currentProcess;
                if (process != null && !process.HasExited && IsReadyForPublish(process))
                    return process;
                try
                {
                    await Task.Delay(_statusCheckInterval, ct);
                }
                catch (OperationCanceledException)
                {
                    if (process != null)
                    {
                        throw CreateUnableToPublishException(process);
                    }
                    throw;
                }
            }
        }

        private async Task WaitUntilReady(Process process, CancellationToken ct)
        {
            if (_stateProvider == null)
                return;

            while (!_stateProvider.IsReady(process))
            {
                await Task.Delay(_statusCheckInterval, ct).ConfigureAwait(false);
            }
        }

        private async Task WaitUntilExit(Process process)
        {
            while (!process.HasExited)
            {
                await Task.Delay(_statusCheckInterval).ConfigureAwait(false);
            }
        }

        private void OnCancellation(Process process)
        {
            using var logScope = BeginProcessLoggingScope(process);
            try
            {
                if (process.HasExited)
                    return;

                if (_options.GracefulStopTimeout != null)
                {
                    if (process.WaitForExit((int)Math.Ceiling(_options.GracefulStopTimeout.Value.TotalMilliseconds)))
                    {
                        return;
                    }
                }

                process.Kill();
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Cancellation failed on process {process_id} shard", process.Id);
            }
        }
        private Exception CreateProcessExitCodeException(Process process)
        {
            return new ProcessShardException($"Process {process.Id} shard suddenly stopped with exit code {process.ExitCode}.", _shardId, process.Id, process.ExitCode);
        }

        private Exception CreateUnableToPublishException(Process process)
        {
            return new ProcessShardException($"Unable to publish messages because publish was cancelled waiting for process to start. Last time process {process.Id} stopped with exit code {process.ExitCode}.", _shardId, process.Id, process.ExitCode);
        }

        private Exception CreatePublishedWithErrorException(Process process)
        {
            return new ProcessShardException($"Publish was successful but process crashed. Last time process {process.Id} stopped with exit code {process.ExitCode}.", _shardId, process.Id, process.ExitCode);
        }

        private Exception CreateShardDisposedException()
        {
            return new ObjectDisposedException(_shardId, "Shard is disposed.");
        }

        private async Task ProcessStreamReader<T>(StreamReader input, IStreamReaderDeserializer<T> deserializer, Channel<ShardMessage<T>> output, Process process, CancellationToken ct)
        {
            await Task.Yield();
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    await foreach (var item in deserializer.GetDeserializedEnumerableAsync(input, ct)
                                       .ConfigureAwait(false))
                    {
                        await output.Writer.WriteAsync(new ShardMessage<T>
                        {
                            Timestamp = DateTime.UtcNow,
                            Value = item
                        }, ct).ConfigureAwait(false);
                    }
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested)
                {
                    break;
                }
                catch (ObjectDisposedException ode) when (ode.ObjectName == _shardId)
                {
                    break;
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Failed to deserialize from stream reader for process {process_id} shard", process.Id);
                }
            }
        }

        public void Dispose()
        {
            if(_disposed)
                return;

            _outputs.Writer.Complete(CreateShardDisposedException());
            _errors.Writer.Complete(CreateShardDisposedException());
            _publishSemaphore.Dispose();
            _disposed = true;
        }
    }
}
