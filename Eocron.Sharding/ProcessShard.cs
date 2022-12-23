using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
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
        private Process _currentProcess;

        public ProcessShard(ProcessStartInfo startInfo,
            IStreamReaderDeserializer<TOutput> outputDeserializer,
            IStreamReaderDeserializer<TError> errorDeserializer,
            IStreamWriterSerializer<TInput> inputSerializer,
            ILogger logger,
            TimeSpan? gracefulStopTimeout = null,
            TimeSpan? statusCheckInterval = null)
        {
            _startInfo = startInfo ?? throw new ArgumentNullException(nameof(startInfo));
            _outputDeserializer = outputDeserializer ?? throw new ArgumentNullException(nameof(outputDeserializer));
            _errorDeserializer = errorDeserializer ?? throw new ArgumentNullException(nameof(errorDeserializer));
            _inputSerializer = inputSerializer ?? throw new ArgumentNullException(nameof(inputSerializer)); 
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            var options = new DataflowBlockOptions
            {
                EnsureOrdered = true,
                BoundedCapacity = 10000,
            };
            _outputs = new BufferBlock<TOutput>(options);
            _errors = new BufferBlock<TError>(options);

            _gracefulStopTimeout = gracefulStopTimeout;
            _statusCheckInterval = statusCheckInterval ?? TimeSpan.FromMilliseconds(100);

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
            catch(Exception ex)
            {
                _logger.LogCritical(ex, "Cancellation failed on process {processId} shard", process.Id);
            }
        }

        public IAsyncEnumerable<TOutput> GetOutputEnumerable(CancellationToken ct)
        {
            return GetAsyncEnumerable(_outputs, ct);
        }

        public IAsyncEnumerable<TError> GetErrorsEnumerable(CancellationToken ct)
        {
            return GetAsyncEnumerable(_errors, ct);
        }

        public async Task PublishAsync(IEnumerable<TInput> messages, CancellationToken ct)
        {
            var process = _currentProcess;
            if (process == null)
                throw CreateProcessNotRunningException();
            foreach (var message in messages)
            {
                ct.ThrowIfCancellationRequested();
                await _inputSerializer.SerializeTo(process.StandardInput, message, ct).ConfigureAwait(false);
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
            try
            {
                while (!process.HasExited)
                {
                    await Task.Delay(_statusCheckInterval).ConfigureAwait(false);
                }
            }
            finally
            {
                _currentProcess = null;
            }
            cts.Cancel();
            await Task.WhenAll(ioTasks);


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

        private static Exception CreateProcessExitCodeException(Process process)
        {
            var tmp = new Exception($"Process {process.Id} shard suddenly stopped with exit code {process.ExitCode}.");
            tmp.Data["processId"] = process.Id;
            tmp.Data["exitCode"] = process.ExitCode;
            return tmp;
        }

        private static Exception CreateProcessNotRunningException()
        {
            var tmp = new Exception($"Process not running.");
            return tmp;
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

        private static async IAsyncEnumerable<T> GetAsyncEnumerable<T>(BufferBlock<T> output, [EnumeratorCancellation] CancellationToken ct)
        {
            while (true)
            {
                ct.ThrowIfCancellationRequested();
                yield return await output.ReceiveAsync(ct).ConfigureAwait(false);
            }
        }
    }
}
