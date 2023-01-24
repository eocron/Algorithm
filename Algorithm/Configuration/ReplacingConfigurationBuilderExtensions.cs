using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Eocron.Algorithms.Configuration;
using Microsoft.Extensions.Configuration;

namespace Eocron.Algorithms
{
    public static class ReplacingConfigurationBuilderExtensions
    {
        public static readonly Regex DefaultNamePattern = new Regex(@"{(?<name>[a-z0-9_\-\.]+?)}", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        /// <summary>
        /// Replace {name} matches in configuration values to map[name]
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="map"></param>
        /// <param name="throwIfNotFound">If true - replacer will throw error on not found names</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static IConfigurationBuilder AddDefaultPatternReplacing(this IConfigurationBuilder builder, IEnumerable<KeyValuePair<string, string>> map, bool throwIfNotFound = false)
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));
            if (map == null)
                throw new ArgumentNullException(nameof(map));

            var reps = new Dictionary<string, string>(map);
            return AddPatternReplacing(
                builder,
                DefaultNamePattern,
                x =>
                {
                    var name = x.Groups["name"].Value;
                    if (reps.TryGetValue(name, out var newValue))
                        return newValue;
                    if (throwIfNotFound)
                        throw new KeyNotFoundException($"Key '{name}' not found when replacing.");
                    return x.Value;
                });
        }

        public static IConfigurationBuilder AddPatternReplacing(this IConfigurationBuilder builder, Regex pattern, MatchEvaluator evaluator)
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));
            if(pattern == null)
                throw new ArgumentNullException(nameof(pattern));
            if (evaluator == null)
                throw new ArgumentNullException(nameof(evaluator));

            for (int i = 0; i < builder.Sources.Count; i++)
            {
                builder.Sources[i] = new ReplacingConfigurationSource(builder.Sources[i], pattern, evaluator);
            }
            return builder;
        }
    }
}