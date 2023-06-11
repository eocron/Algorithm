using System;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;

namespace Eocron.Algorithms.Configuration
{
    public sealed class ReplacingConfigurationSource : IConfigurationSource
    {
        public ReplacingConfigurationSource(IConfigurationSource inner, Regex pattern, MatchEvaluator evaluator)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
            _pattern = pattern ?? throw new ArgumentNullException(nameof(pattern));
            _evaluator = evaluator ?? throw new ArgumentNullException(nameof(evaluator));
        }

        public IConfigurationProvider Build(IConfigurationBuilder builder)
        {
            return new ReplacingConfigurationProvider(_inner.Build(builder), _pattern, _evaluator);
        }

        private readonly IConfigurationSource _inner;
        private readonly MatchEvaluator _evaluator;
        private readonly Regex _pattern;
    }
}