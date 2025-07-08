using API.DataAccess.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using ProductAPI.DataAccess.Context;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace ProductAPI.DataAccess.Repositories.Implementations
{
    public class GenericRepository<T> : IGenericRepository<T> where T : class
    {
        protected readonly ApplicationDbContext _context;
        protected readonly DbSet<T> _dbSet;

        public GenericRepository(ApplicationDbContext context)
        {
            _context = context;
            _dbSet = context.Set<T>();
        }

        // Basic CRUD Operations
        public virtual async Task<T?> GetByIdAsync(int id)
        {
            return await _dbSet.FindAsync(id);
        }

        public virtual async Task<T?> GetByIdAsync(int id, params Expression<Func<T, object>>[] includes)
        {
            IQueryable<T> query = _dbSet;

            foreach (var include in includes)
            {
                query = query.Include(include);
            }

            return await query.FirstOrDefaultAsync(entity => EF.Property<int>(entity, "Id") == id);
        }

        public virtual async Task<IEnumerable<T>> GetAllAsync()
        {
            return await _dbSet.ToListAsync();
        }

        public virtual async Task<IEnumerable<T>> GetAllAsync(params Expression<Func<T, object>>[] includes)
        {
            IQueryable<T> query = _dbSet;

            foreach (var include in includes)
            {
                query = query.Include(include);
            }

            return await query.ToListAsync();
        }

        // Query Operations
        public virtual async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate)
        {
            return await _dbSet.Where(predicate).ToListAsync();
        }

        public virtual async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate, params Expression<Func<T, object>>[] includes)
        {
            IQueryable<T> query = _dbSet;

            foreach (var include in includes)
            {
                query = query.Include(include);
            }

            return await query.Where(predicate).ToListAsync();
        }

        public virtual async Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate)
        {
            return await _dbSet.FirstOrDefaultAsync(predicate);
        }

        public virtual async Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate, params Expression<Func<T, object>>[] includes)
        {
            IQueryable<T> query = _dbSet;

            foreach (var include in includes)
            {
                query = query.Include(include);
            }

            return await query.FirstOrDefaultAsync(predicate);
        }

        // Paging
        public virtual async Task<IEnumerable<T>> GetPagedAsync(int pageNumber, int pageSize)
        {
            return await _dbSet
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public virtual async Task<IEnumerable<T>> GetPagedAsync(int pageNumber, int pageSize, Expression<Func<T, bool>>? predicate = null)
        {
            IQueryable<T> query = _dbSet;

            if (predicate != null)
            {
                query = query.Where(predicate);
            }

            return await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        // Paging with Total Count
        public virtual async Task<(IEnumerable<T> Items, int TotalCount)> GetPagedWithCountAsync(int pageNumber, int pageSize)
        {
            var totalCount = await _dbSet.CountAsync();

            var items = await _dbSet
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        // Helper methods for derived repositories
        protected IQueryable<T> ApplyOrdering<TKey>(IQueryable<T> query, Expression<Func<T, TKey>> orderBy, bool ascending = true)
        {
            return ascending ? query.OrderBy(orderBy) : query.OrderByDescending(orderBy);
        }

        protected async Task<(IEnumerable<T> Items, int TotalCount)> ExecutePagedQueryAsync(IQueryable<T> query, int pageNumber, int pageSize)
        {
            var totalCount = await query.CountAsync();

            var items = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        public virtual async Task<(IEnumerable<T> Items, int TotalCount)> GetPagedWithCountAsync(int pageNumber, int pageSize, Expression<Func<T, bool>>? predicate = null)
        {
            IQueryable<T> query = _dbSet;

            if (predicate != null)
            {
                query = query.Where(predicate);
            }

            var totalCount = await query.CountAsync();

            var items = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        public virtual async Task<(IEnumerable<T> Items, int TotalCount)> GetPagedWithCountAsync<TKey>(
            int pageNumber,
            int pageSize,
            Expression<Func<T, TKey>> orderBy,
            bool ascending = true,
            Expression<Func<T, bool>>? predicate = null)
        {
            IQueryable<T> query = _dbSet;

            // Apply filter if provided
            if (predicate != null)
            {
                query = query.Where(predicate);
            }

            // Apply ordering
            if (ascending)
            {
                query = query.OrderBy(orderBy);
            }
            else
            {
                query = query.OrderByDescending(orderBy);
            }

            // Get total count before paging
            var totalCount = await query.CountAsync();

            // Apply paging
            var items = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        // Count Operations
        public virtual async Task<int> CountAsync()
        {
            return await _dbSet.CountAsync();
        }

        public virtual async Task<int> CountAsync(Expression<Func<T, bool>> predicate)
        {
            return await _dbSet.CountAsync(predicate);
        }

        public virtual async Task<bool> ExistsAsync(int id)
        {
            return await _dbSet.FindAsync(id) != null;
        }

        public virtual async Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate)
        {
            return await _dbSet.AnyAsync(predicate);
        }

        // Modification Operations
        public virtual async Task<T> AddAsync(T entity)
        {
            await _dbSet.AddAsync(entity);
            return entity;
        }

        public virtual async Task<IEnumerable<T>> AddRangeAsync(IEnumerable<T> entities)
        {
            await _dbSet.AddRangeAsync(entities);
            return entities;
        }

        public virtual async Task UpdateAsync(T entity)
        {
            _dbSet.Update(entity);
        }

        public virtual async Task UpdateRangeAsync(IEnumerable<T> entities)
        {
            _dbSet.UpdateRange(entities);
        }

        public virtual async Task DeleteAsync(T entity)
        {
            _dbSet.Remove(entity);
        }

        public virtual async Task DeleteAsync(int id)
        {
            var entity = await GetByIdAsync(id);
            if (entity != null)
            {
                _dbSet.Remove(entity);
            }
        }

        public virtual async Task DeleteRangeAsync(IEnumerable<T> entities)
        {
            _dbSet.RemoveRange(entities);
        }

        // Ordering
        public virtual async Task<IEnumerable<T>> GetOrderedAsync<TKey>(Expression<Func<T, TKey>> orderBy, bool ascending = true)
        {
            if (ascending)
            {
                return await _dbSet.OrderBy(orderBy).ToListAsync();
            }
            else
            {
                return await _dbSet.OrderByDescending(orderBy).ToListAsync();
            }
        }

        public virtual async Task<IEnumerable<T>> GetOrderedAsync<TKey>(Expression<Func<T, bool>> predicate, Expression<Func<T, TKey>> orderBy, bool ascending = true)
        {
            var query = _dbSet.Where(predicate);

            if (ascending)
            {
                return await query.OrderBy(orderBy).ToListAsync();
            }
            else
            {
                return await query.OrderByDescending(orderBy).ToListAsync();
            }
        }
    }
}