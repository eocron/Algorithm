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
        private readonly ProcessStartInfo _startInfo;
        private readonly TimeSpan? _gracefulStopTimeout;
        private readonly TimeSpan _statusCheckInterval;
        private readonly IStreamReaderDeserializer<TOutput> _outputDeserializer;
        private readonly IStreamReaderDeserializer<TError> _errorDeserializer;
        private readonly IStreamWriterSerializer<TInput> _inputSerializer;
        private readonly ILogger _logger;

        private readonly BufferBlock<TOutput> _outputs;
        private readonly BufferBlock<TError> _errors;
        private readonly SemaphoreSlim _publishSemaphore;
        public IReceivableSourceBlock<TOutput> Outputs => _outputs;
        public IReceivableSourceBlock<TError> Errors => _errors;
        private Process _currentProcess;

        public ProcessShard(
            ProcessStartInfo startInfo,
            IStreamReaderDeserializer<TOutput> outputDeserializer,
            IStreamReaderDeserializer<TError> errorDeserializer,
            IStreamWriterSerializer<TInput> inputSerializer,
            ILogger logger,
            TimeSpan? gracefulStopTimeout = null,
            TimeSpan? statusCheckInterval = null,
            DataflowBlockOptions outputOptions = null,
            DataflowBlockOptions errorOptions = null)
        {
            _startInfo = startInfo ?? throw new ArgumentNullException(nameof(startInfo));
            _outputDeserializer = outputDeserializer ?? throw new ArgumentNullException(nameof(outputDeserializer));
            _errorDeserializer = errorDeserializer ?? throw new ArgumentNullException(nameof(errorDeserializer));
            _inputSerializer = inputSerializer ?? throw new ArgumentNullException(nameof(inputSerializer)); 
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _outputs = new BufferBlock<TOutput>(outputOptions ?? new DataflowBlockOptions
            {
                EnsureOrdered = true,
                BoundedCapacity = -1,
            });
            _errors = new BufferBlock<TError>(errorOptions ?? new DataflowBlockOptions
            {
                EnsureOrdered = false,
                BoundedCapacity = 10000,
            });
            
            _gracefulStopTimeout = gracefulStopTimeout;
            _statusCheckInterval = statusCheckInterval ?? TimeSpan.FromMilliseconds(100);
            _publishSemaphore = new SemaphoreSlim(1);
        }

        public bool IsReadyForPublish()
        {
            return _currentProcess != null && _publishSemaphore.CurrentCount > 0;
        }

        public async Task PublishAsync(IEnumerable<TInput> messages, CancellationToken ct)
        {
            await _publishSemaphore.WaitAsync(ct);
            try
            {
                var process = await GetRunningProcessAsync(ct).ConfigureAwait(false);
                await _inputSerializer.SerializeTo(process.StandardInput, messages, ct).ConfigureAwait(false);
            }
            finally
            {
                _publishSemaphore.Release();
            }
        }

        private async Task<Process> GetRunningProcessAsync(CancellationToken ct)
        {
            while (true)
            {
                var process = _currentProcess;
                if (process != null && !process.HasExited)
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

        public async Task RunAsync(CancellationToken stopToken)
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(stopToken);
            using var process = Process.Start(_startInfo);
            using var register = cts.Token.Register(()=> OnCancellation(process));
            var ioTasks = new[]
            {
                ProcessStreamReader(process.StandardOutput, _outputDeserializer, _outputs, process, cts.Token),
                ProcessStreamReader(process.StandardError, _errorDeserializer, _errors, process, cts.Token)
            };
            _currentProcess = process;
            while (!process.HasExited)
            {
                await Task.Delay(_statusCheckInterval).ConfigureAwait(false);
            }

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
                _logger.LogError("Process {processId} shard suddenly stopped with exit code {exitCode}", process.Id, process.ExitCode);
                throw CreateProcessExitCodeException(process);
            }
        }
        private void OnCancellation(Process process)
        {
            try
            {
                if (process.HasExited)
                    return;

                if (_gracefulStopTimeout != null)
                {
                    if (process.WaitForExit((int)Math.Ceiling(_gracefulStopTimeout.Value.TotalMilliseconds)))
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

        private Exception CreateUnableToPublishException(Process process)
        {
            return new ProcessShardException($"Unable to publish messages because publish was cancelled waiting for process to start. Last time process {process.Id} stopped with exit code {process.ExitCode}.", process.Id, process.ExitCode);
        }

        private static Exception CreateProcessNotRunningException(Process process)
        {
            if (process == null)
            {
                return new ProcessShardException($"Process not running.", null, null);
            }
            return CreateProcessExitCodeException(process);
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
