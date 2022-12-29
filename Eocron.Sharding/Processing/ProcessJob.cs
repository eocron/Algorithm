using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Eocron.Sharding.Configuration;
using Microsoft.Extensions.Logging;

namespace Eocron.Sharding.Processing
{
    public sealed class ProcessJob<TInput, TOutput, TError> : IProcessJob<TInput, TOutput, TError>
    {
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
            _outputs = Channel.CreateBounded<ShardMessage<TOutput>>(_options.OutputOptions ??
                                                                    ProcessShardOptions.DefaultOutputOptions);
            _errors = Channel.CreateBounded<ShardMessage<TError>>(_options.ErrorOptions ??
                                                                  ProcessShardOptions.DefaultErrorOptions);
            _statusCheckInterval =
                _options.ProcessStatusCheckInterval ?? ProcessShardOptions.DefaultProcessStatusCheckInterval;
            _publishSemaphore = new SemaphoreSlim(1);
            Id = id ?? $"process_shard_{Guid.NewGuid():N}";
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _outputs.Writer.Complete(CreateShardDisposedException());
            _errors.Writer.Complete(CreateShardDisposedException());
            _publishSemaphore.Dispose();
            _disposed = true;
        }

        public Task<bool> IsReadyAsync(CancellationToken ct)
        {
            var process = _currentProcess;
            return Task.FromResult(ProcessHelper.IsAlive(process)
                                   && _publishSemaphore.CurrentCount > 0
                                   && IsReadyForPublish(process));
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
                if (ProcessHelper.IsDead(process) && process.ExitCode != 0) throw CreatePublishedWithErrorException(process);
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
            var processId = ProcessHelper.GetId(process);

            using var register = cts.Token.Register(() => OnCancellation(process));
            using var logScope = BeginProcessLoggingScope(process);

            if (_watcher != null && processId != null)
                await _watcher.ChildrenToWatch.Writer.WriteAsync(processId.Value, cts.Token).ConfigureAwait(false);

            _logger.LogInformation("Process {process_id} shard started", processId);
            await WaitUntilReady(process, cts.Token).ConfigureAwait(false);
            _logger.LogInformation("Process {process_id} shard ready for publish", processId);
            var ioTasks = new[]
            {
                ProcessStreamReader(process.StandardOutput, _outputDeserializer, _outputs, process, cts.Token),
                ProcessStreamReader(process.StandardError, _errorDeserializer, _errors, process, cts.Token)
            };
            _currentProcess = process;
            await WaitUntilExit(process);
            var exitCode = ProcessHelper.GetExitCode(process) ?? -1;
            cts.Cancel();
            await Task.WhenAll(ioTasks).ConfigureAwait(false);

            if (stopToken.IsCancellationRequested)
            {
                if (exitCode == 0)
                    _logger.LogInformation("Process {process_id} shard gracefully cancelled", processId);
                else
                    _logger.LogWarning("Process {process_id} shard cancelled with exit code {exit_code}",
                        processId, exitCode);
            }
            else
            {
                if (exitCode == 0)
                {
                    _logger.LogWarning("Process {process_id} shard suddenly stopped without error", processId);
                }
                else
                {
                    _logger.LogError("Process {process_id} shard suddenly stopped with exit code {exit_code}",
                        processId, exitCode);
                    throw CreateProcessExitCodeException(processId, exitCode);
                }
            }
        }

        public bool TryGetProcessDiagnosticInfo(out ProcessDiagnosticInfo info)
        {
            info = null;
            var current = _currentProcess;
            if (ProcessHelper.IsDead(current))
                return false;

            info = new ProcessDiagnosticInfo
            {
                PrivateMemorySize64 = current.PrivateMemorySize64,
                TotalProcessorTime = current.TotalProcessorTime,
                WorkingSet64 = current.WorkingSet64,
                ModuleName = current.MainModule.ModuleName
            };
            return true;
        }

        private IDisposable BeginProcessLoggingScope(Process process)
        {
            var processId = ProcessHelper.GetId(process);
            return _logger.BeginScope(new Dictionary<string, string>
            {
                { "process_id", processId?.ToString() },
                { "shard_id", Id }
            });
        }

        private Exception CreateProcessExitCodeException(int? processId, int? exitCode)
        {
            return new ProcessShardException(
                $"Process {processId} shard suddenly stopped with exit code {exitCode}.", Id, processId,
                exitCode);
        }

        private Exception CreatePublishedWithErrorException(Process process)
        {
            var processId = ProcessHelper.GetId(process);
            var exitCode = ProcessHelper.GetExitCode(process);
            return new ProcessShardException(
                $"Publish was successful but process crashed. Last time process {processId} stopped with exit code {exitCode}.",
                Id, processId, exitCode);
        }

        private Exception CreateShardDisposedException()
        {
            return new ObjectDisposedException(Id, "Shard is disposed.");
        }

        private Exception CreateUnableToPublishException(Process process)
        {
            var processId = ProcessHelper.GetId(process);
            var exitCode = ProcessHelper.GetExitCode(process);
            return new ProcessShardException(
                $"Unable to publish messages because publish was cancelled waiting for process to start. Last time process {processId} stopped with exit code {exitCode}.",
                Id, processId, exitCode);
        }

        private async Task<Process> GetRunningProcessAsync(CancellationToken ct)
        {
            while (true)
            {
                var process = _currentProcess;
                if (ProcessHelper.IsAlive(process) && IsReadyForPublish(process))
                    return process;
                try
                {
                    await Task.Delay(_statusCheckInterval, ct);
                }
                catch (OperationCanceledException)
                {
                    if (process != null) throw CreateUnableToPublishException(process);
                    throw;
                }
            }
        }

        private bool IsReadyForPublish(Process process)
        {
            return _stateProvider?.IsReady(process) ?? true;
        }

        private void OnCancellation(Process process)
        {
            var processId = ProcessHelper.GetId(process);
            using var logScope = BeginProcessLoggingScope(process);
            try
            {
                if (ProcessHelper.IsDead(process))
                    return;

                if (_options.GracefulStopTimeout != null)
                    if (process.WaitForExit((int)Math.Ceiling(_options.GracefulStopTimeout.Value.TotalMilliseconds)))
                        return;

                process.Kill();
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Cancellation failed on process {process_id} shard", processId);
            }
        }

        private async Task ProcessStreamReader<T>(StreamReader input, IStreamReaderDeserializer<T> deserializer,
            Channel<ShardMessage<T>> output, Process process, CancellationToken ct)
        {
            await Task.Yield();
            while (!ct.IsCancellationRequested)
                try
                {
                    await foreach (var item in deserializer.GetDeserializedEnumerableAsync(input, ct)
                                       .ConfigureAwait(false))
                        await output.Writer.WriteAsync(new ShardMessage<T>
                        {
                            Timestamp = DateTime.UtcNow,
                            Value = item
                        }, ct).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested)
                {
                    break;
                }
                catch (ObjectDisposedException ode) when (ode.ObjectName == Id)
                {
                    break;
                }
                catch (Exception e)
                {
                    var processId = ProcessHelper.GetId(process);
                    _logger.LogError(e, "Failed to deserialize from stream reader for process {process_id} shard",
                        processId);
                }
        }

        private async Task WaitUntilExit(Process process)
        {
            while (ProcessHelper.IsAlive(process)) await Task.Delay(_statusCheckInterval).ConfigureAwait(false);
        }

        private async Task WaitUntilReady(Process process, CancellationToken ct)
        {
            if (_stateProvider == null)
                return;

            while (!_stateProvider.IsReady(process)) await Task.Delay(_statusCheckInterval, ct).ConfigureAwait(false);
        }

        public ChannelReader<ShardMessage<TError>> Errors => _errors.Reader;

        public ChannelReader<ShardMessage<TOutput>> Outputs => _outputs.Reader;

        public string Id { get; }
        private readonly Channel<ShardMessage<TError>> _errors;
        private readonly Channel<ShardMessage<TOutput>> _outputs;
        private readonly IChildProcessWatcher _watcher;
        private readonly ILogger _logger;
        private readonly IProcessStateProvider _stateProvider;

        private readonly IStreamReaderDeserializer<TError> _errorDeserializer;
        private readonly IStreamReaderDeserializer<TOutput> _outputDeserializer;
        private readonly IStreamWriterSerializer<TInput> _inputSerializer;
        private readonly ProcessShardOptions _options;
        private readonly SemaphoreSlim _publishSemaphore;
        private readonly TimeSpan _statusCheckInterval;
        private bool _disposed;
        private Process _currentProcess;
    }
}