using FluxoCaixa.Consolidado.Application.SaldoDiario;
using FluxoCaixa.Consolidado.Domain.Enums;
using FluxoCaixa.Contracts;
using MassTransit;
using MediatR;

namespace FluxoCaixa.Consolidado.Infrastructure.Messaging;

public class LancamentoRegistradoConsumer : IConsumer<LancamentoRegistrado>
{
    private readonly IMediator _mediator;

    public LancamentoRegistradoConsumer(IMediator mediator) => _mediator = mediator;

    public async Task Consume(ConsumeContext<LancamentoRegistrado> context)
    {
        var evento = context.Message;

        var tipo = evento.Tipo == TipoLancamentoContrato.Credito ? TipoMovimento.Credito : TipoMovimento.Debito;

        await _mediator.Send(new AplicarLancamentoCommand(evento.EventId, evento.Data, tipo, evento.Valor), context.CancellationToken);
    }
}
