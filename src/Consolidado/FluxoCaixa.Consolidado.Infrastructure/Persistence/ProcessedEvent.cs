namespace FluxoCaixa.Consolidado.Infrastructure.Persistence;

// Registro de idempotência: cada evento consumido só pode ser aplicado uma vez ao saldo.
public class ProcessedEvent
{
    public Guid EventId { get; private set; }
    public DateTime ProcessadoEm { get; private set; }

    private ProcessedEvent()
    {
    }

    public static ProcessedEvent Criar(Guid eventId) => new()
    {
        EventId = eventId,
        ProcessadoEm = DateTime.UtcNow
    };
}
