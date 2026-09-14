using FluxoCaixa.Contracts;
using FluxoCaixa.Lancamentos.Infrastructure.Persistence;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FluxoCaixa.Lancamentos.Infrastructure.Outbox;

// Lê periodicamente a tabela outbox e publica no RabbitMQ. Roda fora da transação HTTP,
// então uma falha temporária no broker não afeta a disponibilidade do serviço de Lançamentos -
// a mensagem só fica pendente e é reprocessada no próximo ciclo (at-least-once).
public class OutboxPublisherService : BackgroundService
{
    private static readonly TimeSpan PollingInterval = TimeSpan.FromSeconds(2);
    private const int BatchSize = 50;

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OutboxPublisherService> _logger;

    public OutboxPublisherService(IServiceScopeFactory scopeFactory, ILogger<OutboxPublisherService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PublicarPendentesAsync(stoppingToken);
                await Task.Delay(PollingInterval, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Falha ao processar a fila de outbox.");
            }
        }
    }

    private async Task PublicarPendentesAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<LancamentosDbContext>();
        var publishEndpoint = scope.ServiceProvider.GetRequiredService<IPublishEndpoint>();

        var pendentes = await dbContext.OutboxMessages
            .Where(m => m.ProcessadoEm == null)
            .OrderBy(m => m.OcorridoEm)
            .Take(BatchSize)
            .ToListAsync(cancellationToken);

        if (pendentes.Count == 0)
            return;

        foreach (var mensagem in pendentes)
        {
            try
            {
                var evento = System.Text.Json.JsonSerializer.Deserialize<LancamentoRegistrado>(mensagem.Conteudo)
                             ?? throw new InvalidOperationException("Não foi possível desserializar o evento da outbox.");

                await publishEndpoint.Publish(evento, cancellationToken);
                mensagem.MarcarProcessado();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Falha ao publicar mensagem {MensagemId} da outbox, será tentada novamente.", mensagem.Id);
                mensagem.RegistrarFalha(ex.Message);
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
