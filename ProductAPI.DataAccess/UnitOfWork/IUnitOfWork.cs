using API.DataAccess.Repositories.Interfaces;
using ProductAPI.DataAccess.Repositories;
using ProductAPI.DataAccess.Repositories.Interfaces;
using ProductAPI.Domain.Entities;

namespace ProductAPI.DataAccess.UnitOfWork
{
    public interface IUnitOfWork : IDisposable
    {
        //Repositories
        IUserRepository Users { get; }
        IGenericRepository<Product> Products { get; }
        IGenericRepository<Order> Orders { get; }
        IGenericRepository<OrderItem> OrderItems { get; }

        //Transactions
        Task<int> SaveChangesAsync();
        Task<int> SaveChangesAsync(CancellationToken cancellationToken);

        //Transaction Control
        Task BeginTransactionAsync();
        Task CommitTransactionAsync();
        Task RollbackTransactionAsync();

        //Bulk Operations
        Task<int> ExecuteSqlRawAsync(string sql, params object[] parameters);
        Task<int> ExecuteSqlRawAsync(string sql, CancellationToken cancellationToken, params object[] parameters);
    }
}
