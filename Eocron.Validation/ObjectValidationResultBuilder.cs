using System;

namespace Eocron.Validation
{
    public class ObjectValidationResultBuilder<T> : ValidationResultBuilder
    {
        public Lazy<T> ObjectProvider { get; set; } = new Lazy<T>(() => throw new ArgumentNullException("Validation object is not set"));

        public Func<T, string> ObjectMessageProvider
        {
            set
            {
                ValidationResultMessageProvider = () => value(ObjectProvider.Value);
            }
        }

        public Func<T, bool> ObjectCondition
        {
            set
            {
                Condition = () => value(ObjectProvider.Value);
            }
        }
    }
}