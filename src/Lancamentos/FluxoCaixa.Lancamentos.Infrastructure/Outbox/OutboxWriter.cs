using System.Text.Json;
using FluxoCaixa.Contracts;
using FluxoCaixa.Lancamentos.Application.Interfaces;
using FluxoCaixa.Lancamentos.Infrastructure.Persistence;

namespace FluxoCaixa.Lancamentos.Infrastructure.Outbox;

public class OutboxWriter : IOutboxWriter
{
    private readonly LancamentosDbContext _dbContext;

    public OutboxWriter(LancamentosDbContext dbContext) => _dbContext = dbContext;

    public Task AdicionarAsync(LancamentoRegistrado evento, CancellationToken cancellationToken)
    {
        var mensagem = OutboxMessage.Criar(nameof(LancamentoRegistrado), JsonSerializer.Serialize(evento));
        _dbContext.OutboxMessages.Add(mensagem);
        return Task.CompletedTask;
    }
}
