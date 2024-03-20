using System;
using System.Collections.Generic;

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
            return new ObjectValidationResultBuilder<T>(){ObjectProvider = new Lazy<T>(objectProvider ?? throw new ArgumentNullException(nameof(objectProvider)))};
        }
        
        public static ObjectValidationResultBuilder<T> IfObject<T>(T obj)
        {
            return IfObject(() => obj);
        }
        
        public static ValidationResultBuilder Or(this IEnumerable<ValidationResult> builder, Func<bool> condition)
        {
            var tmp = If(condition);
            tmp.ValidationResultPrefix = builder;
            return tmp;
        }
        
        public static ObjectValidationResultBuilder<T> OrObject<T>(this IEnumerable<ValidationResult> builder, Func<T> objectProvider)
        {
            var tmp = IfObject(objectProvider);
            tmp.ValidationResultPrefix = builder;
            return tmp;
        }
    }
}