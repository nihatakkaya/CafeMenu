using CafeMenu.Api.Data;

namespace CafeMenu.Api.Repositories;

public sealed class EfUnitOfWork : IUnitOfWork
{
    private readonly CafeMenuDbContext _dbContext;

    public EfUnitOfWork(CafeMenuDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}
