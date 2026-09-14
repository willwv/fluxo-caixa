using FluxoCaixa.Consolidado.Application.Interfaces;
using FluxoCaixa.Consolidado.Domain.Enums;
using FluxoCaixa.Consolidado.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FluxoCaixa.Consolidado.Infrastructure.Repositories;

public class SaldoDiarioWriteRepository : ISaldoDiarioWriteRepository
{
    private readonly ConsolidadoWriteDbContext _dbContext;

    public SaldoDiarioWriteRepository(ConsolidadoWriteDbContext dbContext) => _dbContext = dbContext;

    public async Task<bool> AplicarLancamentoAsync(Guid eventId, DateOnly data, TipoMovimento tipo, decimal valor, CancellationToken cancellationToken)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        var agora = DateTime.UtcNow;

        // Tenta marcar o evento como processado; se já existir (entrega duplicada do broker),
        // 0 linhas são afetadas e não aplicamos o incremento de novo - idempotência.
        var linhasInseridas = await _dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"""
             INSERT INTO processed_events (event_id, processado_em)
             VALUES ({eventId}, {agora})
             ON CONFLICT (event_id) DO NOTHING
             """, cancellationToken);

        if (linhasInseridas == 0)
        {
            await transaction.RollbackAsync(cancellationToken);
            return false;
        }

        var creditoDelta = tipo == TipoMovimento.Credito ? valor : 0m;
        var debitoDelta = tipo == TipoMovimento.Debito ? valor : 0m;

        // UPSERT atômico: soma o delta na linha existente (ou cria a linha do dia com o valor inicial).
        // Evita o padrão "carregar > somar em memória > salvar", que sofreria lost update sob
        // múltiplos consumers concorrentes processando o mesmo dia.
        await _dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"""
             INSERT INTO saldo_diario (data, total_creditos, total_debitos, atualizado_em)
             VALUES ({data}, {creditoDelta}, {debitoDelta}, {agora})
             ON CONFLICT (data) DO UPDATE SET
                 total_creditos = saldo_diario.total_creditos + EXCLUDED.total_creditos,
                 total_debitos = saldo_diario.total_debitos + EXCLUDED.total_debitos,
                 atualizado_em = EXCLUDED.atualizado_em
             """, cancellationToken);

        await transaction.CommitAsync(cancellationToken);
        return true;
    }
}
