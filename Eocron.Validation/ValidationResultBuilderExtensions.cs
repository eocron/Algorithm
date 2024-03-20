using System;
using System.Collections.Generic;

namespace Eocron.Validation
{
    public static class ValidationResultBuilderExtensions
    {
        public static ValidationResultBuilder WithMessage(this ValidationResultBuilder builder,
            string messageFormat, params object[] args)
        {
            return builder.WithMessage(() => string.Format(messageFormat, args));
        }
        public static ValidationResultBuilder WithMessage(this ValidationResultBuilder builder,
            Func<string> messageProvider)
        {
            builder.ValidationResultMessageProvider = messageProvider ?? throw new ArgumentNullException(nameof(messageProvider));
            return builder;
        }

        public static ValidationResultBuilder AsWarning(this ValidationResultBuilder builder,bool condition = true)
        {
            builder.ValidationResultType = condition ? ValidationResultType.Warning : builder.ValidationResultType;
            return builder;
        }
        public static ValidationResultBuilder If(this IEnumerable<ValidationResult> builder, Func<bool> condition)
        {
            return new ValidationResultBuilder
            {
                Condition = condition,
                ValidationResultPrefix = builder
            };
        }
        
        public static ObjectValidationResultBuilder<T> IfObject<T>(this IEnumerable<ValidationResult> builder, Func<T> objectProvider)
        {
            return new ObjectValidationResultBuilder<T>
            {
                ObjectProvider = objectProvider,
                ValidationResultPrefix = builder
            };
        }

        public static ValidationResultBuilder IgnoreIf(this ValidationResultBuilder builder, Func<bool> condition)
        {
            if(builder.IgnoreCondition != null)
                throw new InvalidOperationException($"Validation ignore condition already set");
            builder.IgnoreCondition = condition ?? throw new ArgumentNullException(nameof(condition));
            return builder;
        }
        
        public static ValidationResultBuilder IgnoreIf(this ValidationResultBuilder builder, bool condition)
        {
            if(builder.IgnoreCondition != null)
                throw new InvalidOperationException($"Validation ignore condition already set");
            builder.IgnoreCondition = ()=> condition;
            return builder;
        }
        
        public static ValidationResultBuilder Then(this ValidationResultBuilder builder,
            Func<IEnumerable<ValidationResult>> then)
        {
            if (builder.ThenValidations != null)
                throw new InvalidOperationException($"Validation 'then' branch already set");
            builder.ThenValidations = then ?? throw new ArgumentNullException(nameof(then));
            return builder;
        }
        
        public static ValidationResultBuilder Else(this ValidationResultBuilder builder,
            Func<IEnumerable<ValidationResult>> @else)
        {
            if (builder.ElseValidations != null)
                throw new InvalidOperationException($"Validation 'else' branch already set");
            builder.ElseValidations = @else ?? throw new ArgumentNullException(nameof(@else));
            return builder;
        }

        public static ValidationResultBuilder Then(this ValidationResultBuilder builder,
            IEnumerable<ValidationResult> then)
        {
            if (then == null)
                throw new ArgumentNullException(nameof(then));
            return builder.Then(()=> then);
        }
        
        public static ValidationResultBuilder Else(this ValidationResultBuilder builder,
            IEnumerable<ValidationResult> @else)
        {
            if (@else == null)
                throw new ArgumentNullException(nameof(@else));
            return builder.Else(()=> @else);
        }

        public static ValidationResultBuilder Then(this ValidationResultBuilder builder,
            ValidationResult then)
        {
            if (then == null)
                throw new ArgumentNullException(nameof(then));
            return builder.Then(new[]{then});
        }
        
        public static ValidationResultBuilder Else(this ValidationResultBuilder builder,
            ValidationResult @else)
        {
            if (@else == null)
                throw new ArgumentNullException(nameof(@else));
            return builder.Else(new[]{@else});
        }
    }
}