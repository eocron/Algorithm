using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace Eocron.Algorithms.UI.Editing
{
    public class PropertyEditSessionChange<TDocument, TProperty> : IEditSessionChange<TDocument>
    {
        private readonly Expression<Func<TDocument, TProperty>> _propertySelector;
        private readonly TProperty _newValue;
        private TProperty _oldValue;
        private readonly List<(object parent, PropertyInfo property)> _createdObjects = new();
        
        public PropertyEditSessionChange(
            Expression<Func<TDocument, TProperty>> propertySelector,
            TProperty newValue)
        {
            _propertySelector = propertySelector ?? throw new ArgumentNullException(nameof(propertySelector));
            _newValue = newValue;
        }

        public void Redo(TDocument document)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));

            var properties = GetPropertyChain(_propertySelector);

            object current = document;

            for (var i = 0; i < properties.Count - 1; i++)
            {
                var prop = properties[i];
                var value = prop.GetValue(current);

                if (value == null)
                {
                    value = Activator.CreateInstance(prop.PropertyType)
                            ?? throw new InvalidOperationException(
                                $"Cannot create instance of {prop.PropertyType.FullName}");

                    prop.SetValue(current, value);
                    _createdObjects.Add((current, prop));
                }

                current = value;
            }

            var targetProperty = properties[^1];
            _oldValue = (TProperty)targetProperty.GetValue(current);

            targetProperty.SetValue(current, _newValue);
        }

        public void Undo(TDocument document)
        {
            var properties = GetPropertyChain(_propertySelector);

            object current = document;

            for (int i = 0; i < properties.Count - 1; i++)
            {
                current = properties[i].GetValue(current);
                if (current == null)
                    return;
            }

            properties[^1].SetValue(current, _oldValue);
            
            for (var i = _createdObjects.Count - 1; i >= 0; i--)
            {
                var (parent, property) = _createdObjects[i];
                property.SetValue(parent, null);
            }
            _createdObjects.Clear();
        }

        private static List<PropertyInfo> GetPropertyChain(
            Expression<Func<TDocument, TProperty>> expression)
        {
            var properties = new List<PropertyInfo>();
            Expression current = expression.Body;

            while (current is MemberExpression member)
            {
                if (member.Member is PropertyInfo property)
                    properties.Insert(0, property);
                else
                    throw new InvalidOperationException("Expression contains non-property member.");

                current = member.Expression;
            }

            if (current.NodeType != ExpressionType.Parameter)
                throw new InvalidOperationException("Invalid property selector expression.");

            return properties;
        }
    }
}