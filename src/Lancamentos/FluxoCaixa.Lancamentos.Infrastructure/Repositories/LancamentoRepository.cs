using FluxoCaixa.Lancamentos.Application.Interfaces;
using FluxoCaixa.Lancamentos.Domain.Entities;
using FluxoCaixa.Lancamentos.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FluxoCaixa.Lancamentos.Infrastructure.Repositories;

public class LancamentoRepository : ILancamentoRepository
{
    private readonly LancamentosDbContext _dbContext;

    public LancamentoRepository(LancamentosDbContext dbContext) => _dbContext = dbContext;

    public Task AdicionarAsync(Lancamento lancamento, CancellationToken cancellationToken)
    {
        _dbContext.Lancamentos.Add(lancamento);
        return Task.CompletedTask;
    }

    public Task<Lancamento?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken) =>
        _dbContext.Lancamentos.AsNoTracking().FirstOrDefaultAsync(l => l.Id == id, cancellationToken);

    public async Task<(IReadOnlyList<Lancamento> Itens, int TotalItens)> ListarAsync(
        DateOnly? data, int pagina, int tamanhoPagina, CancellationToken cancellationToken)
    {
        var query = _dbContext.Lancamentos.AsNoTracking().AsQueryable();

        if (data is not null)
            query = query.Where(lancamento => lancamento.Data == data.Value);

        var totalItens = await query.CountAsync(cancellationToken);

        var itens = await query
            .OrderByDescending(lancamento => lancamento.CriadoEm)
            .Skip((pagina - 1) * tamanhoPagina)
            .Take(tamanhoPagina)
            .ToListAsync(cancellationToken);

        return (itens, totalItens);
    }
}
