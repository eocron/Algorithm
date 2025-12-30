using System;
using System.Linq.Expressions;

namespace Eocron.Algorithms.UI.Editing;

public static class EditSessionExtensions
{
    public static void SetProperty<TDocument, TProperty>(this IEditSession<TDocument> editSession,
        Expression<Func<TDocument, TProperty>> propertySelector, TProperty newValue)
    {
        editSession.Apply(new PropertyEditSessionChange<TDocument, TProperty>(
            propertySelector,
            (obj, property, ctx) =>
            {
                ctx.OldValue = property.GetValue(obj);
                property.SetValue(obj, newValue);
            },
            (obj, property, ctx) =>
            {
                property.SetValue(obj, ctx.OldValue);
            }));
    }
}