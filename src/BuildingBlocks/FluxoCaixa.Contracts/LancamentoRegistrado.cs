namespace FluxoCaixa.Contracts;

// Contrato de integração publicado pelo serviço de Lançamentos e consumido pelo serviço de Consolidado.
// Qualquer mudança aqui é uma mudança de contrato entre bounded contexts - versionar com cuidado.
public record LancamentoRegistrado(
    Guid EventId,
    Guid LancamentoId,
    DateOnly Data,
    TipoLancamentoContrato Tipo,
    decimal Valor,
    DateTime OcorreuEm);
