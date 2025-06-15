using System;
using System.Linq;
using System.Linq.Expressions;

namespace Eocron.Algorithms.Queryable.Paging
{
    /// <summary>
    /// Provides extension to efficiently page through any queryable, without full scan
    /// </summary>
    public static class PagingQueryableExtensions
    {
        public static PagingConfiguration<TEntity> OrderBy<TEntity, TKey>(this PagingConfiguration<TEntity> configuration,
            Expression<Func<TEntity, TKey>> orderByExpression)
        {
            configuration.AddKeySelector(orderByExpression);
            return configuration;
        }
        
        public static PagingConfiguration<TEntity> OrderByDescending<TEntity, TKey>(this PagingConfiguration<TEntity> configuration,
            Expression<Func<TEntity, TKey>> orderByExpression)
        {
            configuration.AddKeySelector(orderByExpression, isDescending: true);
            return configuration;
        }
        /// <summary>
        /// Applies configured ordering.
        /// Applies skipping WHERE condition to IQueryable if continuation token is not empty.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="configuration"></param>
        /// <param name="continuationToken"></param>
        /// <typeparam name="TEntity"></typeparam>
        /// <returns></returns>
        public static IQueryable<TEntity> ApplyContinuationTokenFilter<TEntity>(
            this IQueryable<TEntity> source,
            PagingConfiguration<TEntity> configuration, 
            string continuationToken) where TEntity : class
        {
            if(source == null)
                throw new ArgumentNullException(nameof(source));
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));
            
            var result = source;
            result = ApplyOrdering(result, configuration);
            if (string.IsNullOrWhiteSpace(continuationToken))
                return result;
            result = result.Where(CreateSkipCondition(configuration, continuationToken));
            return result;
        }

        private static Expression<Func<TEntity, bool>> CreateSkipCondition<TEntity>(
            PagingConfiguration<TEntity> configuration,
            string continuationToken)
        {
            var keyValues = configuration.GetKeyValues(continuationToken).Select(Expression.Constant).ToList();
            Expression predicate = null;

            for (var i = configuration.Keys.Count - 1; i >= 0; i--)
            {
                var keyCfg = configuration.Keys[i];
                var keyValue = keyValues[i];

                var comparison = keyCfg.IsDescending
                    ? Expression.LessThan(keyCfg.KeySelector.Body, keyValue)
                    : Expression.GreaterThan(keyCfg.KeySelector.Body, keyValue);

                for (var j = 0; j < i; j++)
                {
                    var prevKeyCfg = configuration.Keys[j];
                    var prevKeyValue = keyValues[j];
                    var prevEqual = Expression.Equal(prevKeyCfg.KeySelector.Body, prevKeyValue);
                    comparison = Expression.AndAlso(prevEqual, comparison);
                }

                predicate = predicate == null ? comparison : Expression.OrElse(predicate, comparison);
            }

            var lambda = Expression.Lambda<Func<TEntity, bool>>(predicate!, configuration.Input);
            return lambda;
        }

        private static IQueryable<TEntity> ApplyOrdering<TEntity>(IQueryable<TEntity> queryable,
            PagingConfiguration<TEntity> configuration)
        {
            for (int i = 0; i < configuration.Keys.Count; i++)
            {
                var keyCfg = configuration.Keys[i];
                queryable = ApplyOrdering(queryable, keyCfg.KeySelector, keyCfg.IsDescending, isFirst: i == 0);
            }

            return queryable;
        }

        private static IOrderedQueryable<TEntity> ApplyOrdering<TEntity>(
            IQueryable<TEntity> source,
            LambdaExpression keySelector,
            bool isDescending,
            bool isFirst)
        {
            var methodName = (isFirst, isDescending) switch
            {
                (true, false) => nameof(System.Linq.Queryable.OrderBy),
                (true, true) => nameof(System.Linq.Queryable.OrderByDescending),
                (false, false) => nameof(System.Linq.Queryable.ThenBy),
                (false, true) => nameof(System.Linq.Queryable.ThenByDescending),
            };

            var method = typeof(System.Linq.Queryable)
                .GetMethods()
                .First(m =>
                    m.Name == methodName &&
                    m.GetParameters().Length == 2)
                .MakeGenericMethod(typeof(TEntity), keySelector.Body.Type);

            return (IOrderedQueryable<TEntity>)method.Invoke(null, [source, keySelector])!;
        }
    }
}