using Microsoft.Extensions.Logging;

namespace Eocron.DependencyInjection.Tests
{
    public sealed class TestConsoleLoggerFactory : ILoggerFactory
    {
        public void Dispose()
        {
            // TODO release managed resources here
        }

        public ILogger CreateLogger(string categoryName)
        {
            return new TestConsoleLogger();
        }

        public void AddProvider(ILoggerProvider provider)
        {
            
        }
    }
}