using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;

namespace Eocron.Algorithms.Configuration
{
    public static class ReplacingConfigurationBuilderExtensions
    {
        public static IConfigurationBuilder WithPatternReplacing(this IConfigurationBuilder builder, Regex pattern,
            MatchEvaluator evaluator)
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));
            if (pattern == null)
                throw new ArgumentNullException(nameof(pattern));
            if (evaluator == null)
                throw new ArgumentNullException(nameof(evaluator));

            for (var i = 0; i < builder.Sources.Count; i++)
                builder.Sources[i] = new ReplacingConfigurationSource(builder.Sources[i], pattern, evaluator);
            return builder;
        }

        /// <summary>
        ///     Uses other config values to replace placeholders in format {path:in:placeholder:config}
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="placeholders">Configu which contains placeholder mappings</param>
        /// <param name="throwIfNotFound">Should placeholding throw error if placeholder path not found</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="KeyNotFoundException"></exception>
        public static IConfigurationBuilder WithPlaceholders(this IConfigurationBuilder builder,
            IConfiguration placeholders, bool throwIfNotFound = false)
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));
            if (placeholders == null)
                throw new ArgumentNullException(nameof(placeholders));

            return builder.WithPatternReplacing(
                DefaultPlaceholderNamePattern,
                x =>
                {
                    var key = x.Groups["name"].Value;
                    var res = placeholders[x.Groups["name"].Value];
                    if (res != null) return res;

                    if (throwIfNotFound)
                        throw new KeyNotFoundException($"Placeholder '{key}' not found.");
                    return x.Value;
                });
        }

        public static readonly Regex DefaultPlaceholderNamePattern = new Regex(@"{(?<name>[a-z0-9_\-\.\:\[\]]+?)}",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);
    }
}