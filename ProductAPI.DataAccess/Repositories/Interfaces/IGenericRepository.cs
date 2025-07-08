using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace API.DataAccess.Repositories.Interfaces
{
    public interface IGenericRepository<T> where T : class
    {
        // Basic CRUD Operations
        Task<T?> GetByIdAsync(int id);
        Task<T?> GetByIdAsync(int id, params Expression<Func<T, object>>[] includes);
        Task<IEnumerable<T>> GetAllAsync();
        Task<IEnumerable<T>> GetAllAsync(params Expression<Func<T, object>>[] includes);

        // Query Operations
        Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate);
        Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate, params Expression<Func<T, object>>[] includes);
        Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate);
        Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate, params Expression<Func<T, object>>[] includes);

        // Paging
        Task<IEnumerable<T>> GetPagedAsync(int pageNumber, int pageSize);
        Task<IEnumerable<T>> GetPagedAsync(int pageNumber, int pageSize, Expression<Func<T, bool>>? predicate = null);

        // Paging with Total Count
        Task<(IEnumerable<T> Items, int TotalCount)> GetPagedWithCountAsync(int pageNumber, int pageSize);
        Task<(IEnumerable<T> Items, int TotalCount)> GetPagedWithCountAsync(int pageNumber, int pageSize, Expression<Func<T, bool>>? predicate = null);
        Task<(IEnumerable<T> Items, int TotalCount)> GetPagedWithCountAsync<TKey>(int pageNumber, int pageSize, Expression<Func<T, TKey>> orderBy, bool ascending = true, Expression<Func<T, bool>>? predicate = null);

        // Count Operations
        Task<int> CountAsync();
        Task<int> CountAsync(Expression<Func<T, bool>> predicate);
        Task<bool> ExistsAsync(int id);
        Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate);

        // Modification Operations
        Task<T> AddAsync(T entity);
        Task<IEnumerable<T>> AddRangeAsync(IEnumerable<T> entities);
        Task UpdateAsync(T entity);
        Task UpdateRangeAsync(IEnumerable<T> entities);
        Task DeleteAsync(T entity);
        Task DeleteAsync(int id);
        Task DeleteRangeAsync(IEnumerable<T> entities);

        // Ordering
        Task<IEnumerable<T>> GetOrderedAsync<TKey>(Expression<Func<T, TKey>> orderBy, bool ascending = true);
        Task<IEnumerable<T>> GetOrderedAsync<TKey>(Expression<Func<T, bool>> predicate, Expression<Func<T, TKey>> orderBy, bool ascending = true);
    }
}