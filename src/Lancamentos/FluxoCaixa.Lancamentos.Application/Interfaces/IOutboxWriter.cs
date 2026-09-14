using FluxoCaixa.Contracts;

namespace FluxoCaixa.Lancamentos.Application.Interfaces;

// Enfileira o evento na tabela outbox, dentro da MESMA transação/DbContext do lançamento.
// A publicação de fato no RabbitMQ é feita depois, por um worker separado (OutboxPublisher) -
// isso é o que resolve o problema do "dual write" entre banco e mensageria.
public interface IOutboxWriter
{
    Task AdicionarAsync(LancamentoRegistrado evento, CancellationToken cancellationToken);
}
