using Microsoft.Extensions.Logging;

namespace Eocron.Sharding.ProcessWatcher
{
    public sealed class ConsoleLogger : ILogger
    {
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            var msg = string.Format("[{0}]: {1}", logLevel, formatter(state, exception));
            if (logLevel == LogLevel.Critical || logLevel == LogLevel.Error)
            {
                Console.Error.WriteLine(msg);
            }
            else
            {
                Console.WriteLine(msg);
            }
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull
        {
            return null;
        }
    }
}