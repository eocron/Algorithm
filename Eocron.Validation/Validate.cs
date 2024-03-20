using System;

namespace Eocron.Validation
{
    public static class Validate
    {
        public static ValidationResultBuilder If(Func<bool> condition)
        {
            return new ValidationResultBuilder(){ Condition = condition ?? throw new ArgumentNullException(nameof(condition))};
        }
        
        public static ValidationResultBuilder If(bool condition)
        {
            return If(() => condition);
        }

        public static ObjectValidationResultBuilder<T> IfObject<T>(Func<T> objectProvider)
        {
            return new ObjectValidationResultBuilder<T>(){ObjectProvider = objectProvider ?? throw new ArgumentNullException(nameof(objectProvider))};
        }
    }
}