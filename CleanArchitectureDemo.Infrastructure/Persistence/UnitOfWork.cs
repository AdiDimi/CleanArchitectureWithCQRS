
using CleanArchitectureDemo.Application.Interfaces;
using System.Data.Common;

namespace CleanArchitectureDemo.Infrastructure.Persistence;

public sealed class UnitOfWork : IUnitOfWork, IAsyncDisposable
{
    private readonly IDbConnectionFactory _factory;
    private readonly IDbSessionAccessor _session;

    private DbConnection? _connection;
    private DbTransaction? _transaction;

    public UnitOfWork(IDbConnectionFactory factory, IDbSessionAccessor session)
    {
        _factory = factory;
        _session = session;
    }

    public async Task BeginTransactionAsync(CancellationToken ct = default)
    {
        var conn = (DbConnection)_factory.CreateConnection();
        await conn.OpenAsync(ct);
        var tx = await conn.BeginTransactionAsync(ct);

        _connection = conn;
        _transaction = tx;

        _session.SetSession(_connection, _transaction);
    }

    public async Task CommitTransactionAsync(CancellationToken ct = default)
    {
        if (_transaction == null) return;
        await _transaction.CommitAsync(ct);
        await Cleanup();
    }

    public async Task RollbackTransactionAsync(CancellationToken ct = default)
    {
        if (_transaction == null) return;
        await _transaction.RollbackAsync(ct);
        await Cleanup();
    }

    private async ValueTask Cleanup()
    {
        _session.ClearSession();
        if (_transaction != null) await _transaction.DisposeAsync();
        if (_connection != null) await _connection.DisposeAsync();
    }

    public async ValueTask DisposeAsync() => await Cleanup();
}
