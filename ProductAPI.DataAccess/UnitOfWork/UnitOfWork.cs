using API.DataAccess.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using ProductAPI.DataAccess.Context;
using ProductAPI.DataAccess.Repositories.Implementations;
using ProductAPI.DataAccess.Repositories.Interfaces;
using ProductAPI.Domain.Entities;

namespace ProductAPI.DataAccess.UnitOfWork
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly ApplicationDbContext _context;
        private IDbContextTransaction? _transaction;

        public UnitOfWork(ApplicationDbContext context)
        {
            _context = context;
        }

        // Repository instances (lazy initialization)
        private IUserRepository? _users;

        private IGenericRepository<Product> _products;

        private IGenericRepository<Order> _orders;

        private IGenericRepository<OrderItem> _orderItems;

        //Repository Properties
        public IUserRepository Users => _users ??= new UserRepository(_context);
        public IGenericRepository<Order> Orders => _orders ??= new GenericRepository<Order>(_context);
        public IGenericRepository<OrderItem> OrderItems => _orderItems ??= new GenericRepository<OrderItem>(_context);
        public IGenericRepository<Product> Products => _products ??= new GenericRepository<Product>(_context);

        IGenericRepository<Product> IUnitOfWork.Products => Products;
        IGenericRepository<Order> IUnitOfWork.Orders => Orders;
        IGenericRepository<OrderItem> IUnitOfWork.OrderItems => OrderItems;

        //Save Changes
        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }

        public async Task<int> SaveChangesAsync(CancellationToken cancellationToken)
        {
            return await _context.SaveChangesAsync(cancellationToken);
        }

        // Transaction Control
        public async Task BeginTransactionAsync()
        {
            if (_transaction != null)
            {
                throw new InvalidOperationException("A transaction already in progress.");
            }

            _transaction = await _context.Database.BeginTransactionAsync();
        }

        public async Task CommitTransactionAsync()
        {
            if (_transaction == null)
            {
                throw new InvalidOperationException("There is no transaction in progess.");
            }
            try
            {
                await SaveChangesAsync();
                await _transaction.CommitAsync();
            }
            catch
            {
                await _transaction.RollbackAsync();
                throw;
            }
            finally
            {
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }
        public async Task RollbackTransactionAsync()
        {
            if (_transaction == null)
            {
                throw new InvalidOperationException("There is no transaction in progess.");
            }

            try
            {
                await _transaction.RollbackAsync();
            }
            finally
            {
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }

        //Bulk Operations
        public async Task<int> ExecuteSqlRawAsync(string sql, params object[] parameters)
        {
            return await _context.Database.ExecuteSqlRawAsync(sql, parameters);
        }

        public async Task<int> ExecuteSqlRawAsync(string sql, CancellationToken cancellationToken, params object[] parameters)
        {
            return await _context.Database.ExecuteSqlRawAsync(sql, cancellationToken, parameters);
        }

       
        //Dispose
        public void Dispose()
        {
            _transaction?.Dispose();
            _context?.Dispose();
        }


    }
}
