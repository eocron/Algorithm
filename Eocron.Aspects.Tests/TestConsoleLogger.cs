using System;

namespace Eocron.Aspects.Tests
{
    public sealed class TestConsoleLogger : ILogger
    {
        public static readonly ILogger Instance = new TestConsoleLogger();
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            Console.WriteLine($"[{logLevel}]: {formatter(state, exception)}.{exception}");
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