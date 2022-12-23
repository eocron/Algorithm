using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Microsoft.Extensions.Logging;

namespace Eocron.Sharding
{
    public sealed class ProcessShard<TInput, TOutput, TError> : IShard<TInput, TOutput, TError>
    {
        private readonly ProcessShardOptions _options;
        private readonly TimeSpan _statusCheckInterval;
        private readonly IStreamReaderDeserializer<TOutput> _outputDeserializer;
        private readonly IStreamReaderDeserializer<TError> _errorDeserializer;
        private readonly IStreamWriterSerializer<TInput> _inputSerializer;
        private readonly ILogger _logger;
        private readonly IProcessStateProvider _stateProvider;

        private readonly BufferBlock<TOutput> _outputs;
        private readonly BufferBlock<TError> _errors;
        private readonly SemaphoreSlim _publishSemaphore;
        public IReceivableSourceBlock<TOutput> Outputs => _outputs;
        public IReceivableSourceBlock<TError> Errors => _errors;
        private Process _currentProcess;

        public ProcessShard(
            ProcessShardOptions options,
            IStreamReaderDeserializer<TOutput> outputDeserializer,
            IStreamReaderDeserializer<TError> errorDeserializer,
            IStreamWriterSerializer<TInput> inputSerializer,
            ILogger logger,
            IProcessStateProvider stateProvider = null)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _outputDeserializer = outputDeserializer ?? throw new ArgumentNullException(nameof(outputDeserializer));
            _errorDeserializer = errorDeserializer ?? throw new ArgumentNullException(nameof(errorDeserializer));
            _inputSerializer = inputSerializer ?? throw new ArgumentNullException(nameof(inputSerializer)); 
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _stateProvider = stateProvider;
            _outputs = new BufferBlock<TOutput>(_options.OutputOptions ?? ProcessShardOptions.DefaultOutputOptions);
            _errors = new BufferBlock<TError>(_options.ErrorOptions ?? ProcessShardOptions.DefaultErrorOptions);
            _statusCheckInterval = _options.ProcessStatusCheckInterval ?? ProcessShardOptions.DefaultProcessStatusCheckInterval;
            _publishSemaphore = new SemaphoreSlim(1);
        }

        public bool IsReadyForPublish()
        {
            var process = _currentProcess;
            return process != null 
                   && !process.HasExited 
                   && _publishSemaphore.CurrentCount > 0 
                   && IsReadyForPublish(process);
        }

        public async Task PublishAsync(IEnumerable<TInput> messages, CancellationToken ct)
        {
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
            using var register = cts.Token.Register(()=> OnCancellation(process));
            using var logScope = BeginProcessLoggingScope(process);
            _logger.LogInformation("Process {processId} shard started", process.Id);
            await WaitUntilReady(process, cts.Token).ConfigureAwait(false);
            _logger.LogInformation("Process {processId} shard ready for publish", process.Id);
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
                    _logger.LogInformation("Process {processId} shard gracefully cancelled", process.Id);
                }
                else
                {
                    _logger.LogWarning("Process {processId} shard cancelled with exit code {exitCode}", process.Id, process.ExitCode);
                }
            }
            else
            {
                if (process.ExitCode == 0)
                {
                    _logger.LogWarning("Process {processId} shard suddenly stopped without error", process.Id);
                }
                else
                {
                    _logger.LogError("Process {processId} shard suddenly stopped with exit code {exitCode}", process.Id, process.ExitCode);
                    throw CreateProcessExitCodeException(process);
                }
            }
        }

        private IDisposable BeginProcessLoggingScope(Process process)
        {
            return _logger.BeginScope(new Dictionary<string, string>()
            {
                { "processId", process.Id.ToString() }
            });
        }

        private bool IsReadyForPublish(Process process)
        {
            return _stateProvider?.IsReadyForPublish(process) ?? true;
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
            if(_stateProvider == null)
                return;
            
            while (!_stateProvider.IsReadyForPublish(process))
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
                _logger.LogCritical(ex, "Cancellation failed on process {processId} shard", process.Id);
            }
        }
        private static Exception CreateProcessExitCodeException(Process process)
        {
            return new ProcessShardException($"Process {process.Id} shard suddenly stopped with exit code {process.ExitCode}.", process.Id, process.ExitCode);
        }

        private static Exception CreateUnableToPublishException(Process process)
        {
            return new ProcessShardException($"Unable to publish messages because publish was cancelled waiting for process to start. Last time process {process.Id} stopped with exit code {process.ExitCode}.", process.Id, process.ExitCode);
        }

        private static Exception CreatePublishedWithErrorException(Process process)
        {
            return new ProcessShardException($"Publish was successful but process crashed. Last time process {process.Id} stopped with exit code {process.ExitCode}.", process.Id, process.ExitCode);
        }

        private async Task ProcessStreamReader<T>(StreamReader input, IStreamReaderDeserializer<T> deserializer, BufferBlock<T> output, Process process, CancellationToken ct)
        {
            await Task.Yield();
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    await foreach (var item in deserializer.GetDeserializedEnumerableAsync(input, ct).ConfigureAwait(false))
                    {
                        output.Post(item);
                    }
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Failed to deserialize from stream reader for process {processId} shard", process.Id);
                }
            }
        }

        public void Dispose()
        {
            _outputs.Complete();
            _errors.Complete();
            _publishSemaphore.Dispose();
        }
    }
}
