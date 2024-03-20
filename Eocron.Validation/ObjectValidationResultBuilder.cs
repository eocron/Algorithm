using System;

namespace Eocron.Validation
{
    public class ObjectValidationResultBuilder<T> : ValidationResultBuilder
    {
        public Func<T> ObjectProvider { get; set; } =
            () => throw new ArgumentNullException("Validation object provider is not set");

        public Func<T, bool> ObjectCondition
        {
            set
            {
                Condition = () => value(ObjectProvider());
            }
        }
    }
}