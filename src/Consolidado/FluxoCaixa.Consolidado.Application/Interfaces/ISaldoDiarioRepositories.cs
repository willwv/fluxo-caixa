using FluxoCaixa.Consolidado.Application.Common;
using FluxoCaixa.Consolidado.Domain.Enums;

namespace FluxoCaixa.Consolidado.Application.Interfaces;

// Lado de leitura: consulta a réplica (via cache-aside com Redis na frente). Usado pelo relatório,
// que precisa sustentar as 50 req/s de pico sem sobrecarregar o banco.
public interface ISaldoDiarioReadRepository
{
    Task<SaldoDiarioDto?> ObterAsync(DateOnly data, CancellationToken cancellationToken);
}

// Lado de escrita: aplica o movimento no primary via incremento atômico (UPSERT), com
// verificação de idempotência na mesma transação. Não passa pelo cache nem pela réplica.
public interface ISaldoDiarioWriteRepository
{
    // Retorna false quando o evento já havia sido processado antes (entrega duplicada) - no-op.
    Task<bool> AplicarLancamentoAsync(Guid eventId, DateOnly data, TipoMovimento tipo, decimal valor, CancellationToken cancellationToken);
}
