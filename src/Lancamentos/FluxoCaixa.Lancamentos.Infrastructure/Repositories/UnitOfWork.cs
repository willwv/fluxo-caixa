using FluxoCaixa.Lancamentos.Application.Interfaces;
using FluxoCaixa.Lancamentos.Infrastructure.Persistence;

namespace FluxoCaixa.Lancamentos.Infrastructure.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly LancamentosDbContext _dbContext;

    public UnitOfWork(LancamentosDbContext dbContext) => _dbContext = dbContext;

    public Task SalvarAlteracoesAsync(CancellationToken cancellationToken) =>
        _dbContext.SaveChangesAsync(cancellationToken);
}
