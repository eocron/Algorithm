using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Eocron.Validation
{
    public class ValidationResultBuilder : IEnumerable<ValidationResult>
    {
        public ValidationResultType ValidationResultType { get; set; } = ValidationResultType.Error;
        public Func<string> ValidationResultMessageProvider { get; set; } = () => throw new NotImplementedException("Validation message is not provided");
        public IEnumerable<ValidationResult>? ValidationResultPrefix { get; set; }
        public Func<bool>? IgnoreCondition { get; set; }
        public Func<bool> Condition { get; set; } = () => throw new NotImplementedException("Validation condition is not provided");
        public Func<IEnumerable<ValidationResult>>? ThenValidations { get; set; }
        public Func<IEnumerable<ValidationResult>>? ElseValidations { get; set; }

        private IEnumerable<ValidationResult> GetInternalEnumerable()
        {
            if (ValidationResultPrefix != null)
            {
                foreach (var t in ValidationResultPrefix)
                {
                    yield return t;
                }
            }

            var ignore = IgnoreCondition?.Invoke() ?? false;
            if (ignore)
                yield break;
            
            if (Condition())
            {
                if(ThenValidations == null)
                    yield break;
                foreach (var t in ThenValidations())
                {
                    yield return t;
                }
            }
            else
            {
                if (ValidationResultMessageProvider != null)
                {
                    yield return new ValidationResult() { Message = ValidationResultMessageProvider(), Type = this.ValidationResultType };
                }
                if(ElseValidations == null)
                    yield break;
                foreach (var t in ElseValidations())
                {
                    yield return t;
                }
            }
        }
        public IEnumerator<ValidationResult> GetEnumerator()
        {
            return GetInternalEnumerable().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public static implicit operator ValidationResult?(ValidationResultBuilder builder)
        {
            return builder?.SingleOrDefault();
        }
    }
}