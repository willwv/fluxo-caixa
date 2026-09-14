using FluxoCaixa.Consolidado.Application.Interfaces;
using FluxoCaixa.Consolidado.Application.SaldoDiario;
using FluxoCaixa.Consolidado.Domain.Enums;
using NSubstitute;
using Xunit;

namespace FluxoCaixa.Consolidado.UnitTests.Application;

public class AplicarLancamentoHandlerTests
{
    private readonly ISaldoDiarioWriteRepository _repository = Substitute.For<ISaldoDiarioWriteRepository>();

    [Fact]
    public async Task Handle_DeveDelegarParaORepositorioDeEscrita()
    {
        var handler = new AplicarLancamentoHandler(_repository);
        var eventId = Guid.NewGuid();
        var data = DateOnly.FromDateTime(DateTime.Today);

        await handler.Handle(new AplicarLancamentoCommand(eventId, data, TipoMovimento.Credito, 75m), CancellationToken.None);

        await _repository.Received(1).AplicarLancamentoAsync(eventId, data, TipoMovimento.Credito, 75m, Arg.Any<CancellationToken>());
    }
}
