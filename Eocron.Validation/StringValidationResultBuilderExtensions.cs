using System;
using System.Text.RegularExpressions;

namespace Eocron.Validation
{
    public static class StringValidationResultBuilderExtensions
    {
        public static ValidationResultBuilder Match(this ObjectValidationResultBuilder<string> builder, 
            string pattern,
            RegexOptions options = RegexOptions.Compiled | RegexOptions.Multiline | RegexOptions.ExplicitCapture, 
            TimeSpan? timeout = null)
        {
            if (pattern == null)
                throw new ArgumentNullException(nameof(pattern));
            return builder
                .Is(x => x != null && Regex.IsMatch(x, pattern, options, timeout ?? Regex.InfiniteMatchTimeout))
                .WithMessage(x => $"String '{x}' doesn't match regex pattern '{pattern}'");
        }
    }
}