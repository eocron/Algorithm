using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Newtonsoft.Json;

namespace Eocron.Algorithms.Queryable.Paging
{
    public sealed class PagingConfiguration<TEntity> : IPagingConfiguration<TEntity>
    {
        private readonly List<PagingKeyConfiguration> _keys = new();

        internal ParameterExpression Input = Expression.Parameter(typeof(TEntity), "x");
        internal IReadOnlyList<PagingKeyConfiguration> Keys => _keys;

        private static readonly JsonSerializerSettings JsonSerializerSettings = new JsonSerializerSettings()
        {
            TypeNameHandling = TypeNameHandling.All
        };

        public void AddKeySelector<TKey>(Expression<Func<TEntity, TKey>> keySelector, bool isDescending = false)
        {
            if(keySelector == null)
                throw new ArgumentNullException(nameof(keySelector));
            
            var compiled = keySelector.Compile();
            keySelector = (Expression<Func<TEntity, TKey>>)new ReplaceParameterVisitor(keySelector.Parameters[0], Input).Visit(keySelector);
            _keys.Add(new PagingKeyConfiguration()
            {
                CompiledKeySelector = x => new TypeWrapper<TKey> { Value = compiled(x) },
                KeySelector = keySelector,
                IsDescending = isDescending
            });
        }
        
        public string GetContinuationToken(TEntity entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));
            if(_keys.Count == 0)
                throw new InvalidOperationException("No keys defined.");
            
            return JsonConvert.SerializeObject(_keys.Select(x => x.CompiledKeySelector(entity)).ToList(),
                JsonSerializerSettings);
        }

        internal List<object> GetKeyValues(string continuationToken)
        {
            if (string.IsNullOrWhiteSpace(continuationToken))
                throw new ArgumentNullException(nameof(continuationToken));
            if(_keys.Count == 0)
                throw new InvalidOperationException("No keys defined.");
            
            return JsonConvert.DeserializeObject<List<object>>(continuationToken, JsonSerializerSettings)
                .Cast<ITypeWrapper>()
                .Select(x => x.GetValue())
                .ToList();
        }

        private interface ITypeWrapper
        {
            object GetValue();
        }
        
        public class PagingKeyConfiguration
        {
            public Func<TEntity, object> CompiledKeySelector { get; init; }
            
            public LambdaExpression KeySelector { get; init; }
            
            public bool IsDescending { get; init; }
        }
        
        private sealed class TypeWrapper<TValue> : ITypeWrapper
        {
            public TValue Value { get; init; }
            public object GetValue()
            {
                return Value;
            }
        }
        
        private sealed class ReplaceParameterVisitor : ExpressionVisitor
        {
            private readonly ParameterExpression _oldParam;
            private readonly ParameterExpression _newParam;

            public ReplaceParameterVisitor(ParameterExpression oldParam, ParameterExpression newParam)
            {
                _oldParam = oldParam;
                _newParam = newParam;
            }

            protected override Expression VisitParameter(ParameterExpression node)
            {
                return node == _oldParam ? _newParam : base.VisitParameter(node);
            }
        }
    }
}