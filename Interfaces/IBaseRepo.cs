using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace EFGenericRepository
{
    public interface IBaseRepo<T> : IDisposable where T : class, new()
    {
        T GetEntity(T filledKeys);
        IList<T> GetList(Expression<Func<T, bool>> predicate);
        Task<IList<T>> GetListAsync(Expression<Func<T, bool>> predicate);
        IList<TResult> GetList<TResult>(Expression<Func<T, bool>> predicate, Expression<Func<T, TResult>> selector);
        T Save(T updatedEntity);
        Task<T> SaveAsync(T updatedEntity);
        void Remove(T removedEntity);
        Task RemoveAsync(T removedEntity);
        void Remove(ICollection<T> entities);
        Task RemoveAsync(ICollection<T> entities);
        Task RemoveAsync(Expression<Func<T, bool>> predicate);
        void Save(ICollection<T> entities);
        Task SaveAsync(ICollection<T> entities);
        void Remove(Expression<Func<T, bool>> predicate);
        int Count(Expression<Func<T, bool>> predicate);
        Tout Max<Tout>(Expression<Func<T, bool>> predicate, Expression<Func<T, Tout>> selector) where Tout : struct;
        Tout Min<Tout>(Expression<Func<T, bool>> predicate, Expression<Func<T, Tout>> selector) where Tout : struct;
    }
}
