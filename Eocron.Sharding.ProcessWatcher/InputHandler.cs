using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Eocron.Sharding.ProcessWatcher
{
    public sealed class InputHandler : IWatcherJob
    {
        public InputHandler(ILogger logger, Func<IConfiguration, CancellationToken, Task> commandHandler)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _commandHandler = commandHandler ?? throw new ArgumentNullException(nameof(commandHandler));
        }

        public async Task RunAsync(CancellationToken stopToken)
        {
            await Task.Yield();
            while (!stopToken.IsCancellationRequested)
            {
                var config = ReadCommand();
                try
                {
                    await _commandHandler(config, stopToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (stopToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Failed to process command");
                }
            }
        }

        private IConfiguration ReadCommand()
        {
            var line = Console.ReadLine();
            try
            {
                var args = line?.Split(new[] { ' ', '\t' },
                    StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                if (args == null || args.Length == 0)
                    return null;


                var config = new ConfigurationBuilder()
                    .AddCommandLine(args)
                    .Build();
                return config;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Invalid command: {line}", line);
                return null;
            }
        }

        private readonly Func<IConfiguration, CancellationToken, Task> _commandHandler;
        private readonly ILogger _logger;
    }
}