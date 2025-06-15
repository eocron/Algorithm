using System;
using System.Linq.Expressions;

namespace Eocron.Algorithms.Queryable.Paging
{
    public interface IPagingConfiguration<TEntity>
    {
        void AddKeySelector<TKey>(Expression<Func<TEntity, TKey>> keySelector, bool isDescending = false);
        string GetContinuationToken(TEntity entity);
    }
}