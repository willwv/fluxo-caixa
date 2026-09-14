using FluxoCaixa.Consolidado.Application.Interfaces;
using FluxoCaixa.Consolidado.Domain.Enums;
using MediatR;

namespace FluxoCaixa.Consolidado.Application.SaldoDiario;

// Comando disparado pelo consumer do RabbitMQ ao receber um LancamentoRegistrado.
public record AplicarLancamentoCommand(Guid EventId, DateOnly Data, TipoMovimento Tipo, decimal Valor) : IRequest;

public class AplicarLancamentoHandler : IRequestHandler<AplicarLancamentoCommand>
{
    private readonly ISaldoDiarioWriteRepository _repository;

    public AplicarLancamentoHandler(ISaldoDiarioWriteRepository repository) => _repository = repository;

    public async Task Handle(AplicarLancamentoCommand request, CancellationToken cancellationToken)
    {
        await _repository.AplicarLancamentoAsync(request.EventId, request.Data, request.Tipo, request.Valor, cancellationToken);
    }
}
