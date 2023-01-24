using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;

namespace Eocron.Algorithms.Configuration
{
    public sealed class ReplacingConfigurationProvider : IConfigurationProvider
    {
        private readonly IConfigurationProvider _inner;
        private readonly Regex _pattern;
        private readonly MatchEvaluator _evaluator;

        public ReplacingConfigurationProvider(IConfigurationProvider innerProvider, Regex pattern, MatchEvaluator evaluator)
        {
            _inner = innerProvider ?? throw new ArgumentNullException(nameof(innerProvider));
            _pattern = pattern ?? throw new ArgumentNullException(nameof(pattern));
            _evaluator = evaluator ?? throw new ArgumentNullException(nameof(evaluator));
        }

        public bool TryGet(string key, out string? value)
        {
            if (_inner.TryGet(key, out value))
            {
                value = TryReplace(value);
                return true;
            }
            return false;
        }

        public void Set(string key, string? value)
        {
            _inner.Set(key, TryReplace(value));
        }

        public IChangeToken GetReloadToken()
        {
            return _inner.GetReloadToken();
        }

        public void Load()
        {
            _inner.Load();
        }

        public IEnumerable<string> GetChildKeys(IEnumerable<string> earlierKeys, string? parentPath)
        {
            return _inner.GetChildKeys(earlierKeys, parentPath);
        }

        private string? TryReplace(string? input)
        {
            if (input == null)
                return null;
            return _pattern.Replace(input, _evaluator);
        }
    }
}