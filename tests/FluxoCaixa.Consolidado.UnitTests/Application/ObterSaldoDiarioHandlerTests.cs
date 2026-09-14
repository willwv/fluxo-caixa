using FluentAssertions;
using FluxoCaixa.Consolidado.Application.Common;
using FluxoCaixa.Consolidado.Application.Interfaces;
using FluxoCaixa.Consolidado.Application.SaldoDiario;
using NSubstitute;
using Xunit;

namespace FluxoCaixa.Consolidado.UnitTests.Application;

public class ObterSaldoDiarioHandlerTests
{
    private readonly ISaldoDiarioReadRepository _repository = Substitute.For<ISaldoDiarioReadRepository>();

    [Fact]
    public async Task Handle_QuandoExisteSaldo_DeveRetornarDoRepositorio()
    {
        var data = DateOnly.FromDateTime(DateTime.Today);
        var esperado = new SaldoDiarioDto(data, 300m, 100m, 200m, DateTime.UtcNow);
        _repository.ObterAsync(data, Arg.Any<CancellationToken>()).Returns(esperado);

        var resultado = await new ObterSaldoDiarioHandler(_repository).Handle(new ObterSaldoDiarioQuery(data), CancellationToken.None);

        resultado.Should().Be(esperado);
    }

    [Fact]
    public async Task Handle_QuandoNaoExisteSaldo_DeveRetornarNulo()
    {
        var data = DateOnly.FromDateTime(DateTime.Today);
        _repository.ObterAsync(data, Arg.Any<CancellationToken>()).Returns((SaldoDiarioDto?)null);

        var resultado = await new ObterSaldoDiarioHandler(_repository).Handle(new ObterSaldoDiarioQuery(data), CancellationToken.None);

        resultado.Should().BeNull();
    }
}
