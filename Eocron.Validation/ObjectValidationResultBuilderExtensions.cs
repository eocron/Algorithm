using System;

namespace Eocron.Validation
{
    public static class ObjectValidationResultBuilderExtensions
    {
        public static ObjectValidationResultBuilder<T> WithMessage<T>(this ObjectValidationResultBuilder<T> builder,
            Func<T, string> messageProvider)
        {
            builder.ObjectMessageProvider = messageProvider;
            return builder;
        }
        
        public static ObjectValidationResultBuilder<T> Is<T>(this ObjectValidationResultBuilder<T> builder,
            Func<T, bool> conditionProvider)
        {
            builder.ObjectCondition = conditionProvider ?? throw new ArgumentNullException(nameof(conditionProvider));
            return builder;
        }

        public static ValidationResultBuilder NotNull<T>(this ObjectValidationResultBuilder<T> builder)
            where T : class
        {
            return builder.Is(x => x != null)
                .WithMessage(()=> "Object is null");
        }
        
        public static ValidationResultBuilder Null<T>(this ObjectValidationResultBuilder<T> builder)
            where T : class
        {
            return builder.Is(x => x == null)
                .WithMessage(() => "Object is not null");
        }
    }
}