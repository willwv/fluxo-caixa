using FluxoCaixa.Consolidado.Application.Common;
using FluxoCaixa.Consolidado.Application.Interfaces;
using FluxoCaixa.Consolidado.Infrastructure.Cache;
using FluxoCaixa.Consolidado.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FluxoCaixa.Consolidado.Infrastructure.Repositories;

// Cache-aside: o repositório sabe sua própria chave de cache e como popular a partir da read replica em caso de miss;
// Quem decide TTL, serialização e a interação com o Redis em si é o
// IRedisCache (genérico, reutilizável por qualquer outro repositório que precise do mesmo padrão).
public class SaldoDiarioReadRepository : ISaldoDiarioReadRepository
{
    private readonly ConsolidadoReadDbContext _dbContext;
    private readonly IRedisCache _cache;

    public SaldoDiarioReadRepository(ConsolidadoReadDbContext dbContext, IRedisCache cache)
    {
        _dbContext = dbContext;
        _cache = cache;
    }

    public Task<SaldoDiarioDto?> ObterAsync(DateOnly data, CancellationToken cancellationToken) =>
        _cache.ObterAsync(ChaveCache(data), ct => ObterSaldoDiarioAsync(data, ct), cancellationToken);

    private static string ChaveCache(DateOnly data) => $"consolidado:saldo-diario:{data:yyyy-MM-dd}";

    private Task<SaldoDiarioDto?> ObterSaldoDiarioAsync(DateOnly data, CancellationToken cancellationToken) =>
        _dbContext.SaldosDiarios.AsNoTracking()
            .Where(s => s.Data == data)
            .Select(s => new SaldoDiarioDto(s.Data, s.TotalCreditos, s.TotalDebitos, s.TotalCreditos - s.TotalDebitos, s.AtualizadoEm))
            .FirstOrDefaultAsync(cancellationToken);
}
