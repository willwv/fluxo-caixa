using FluentAssertions;
using FluxoCaixa.Lancamentos.Application.Interfaces;
using FluxoCaixa.Lancamentos.Application.Lancamentos;
using FluxoCaixa.Lancamentos.Domain.Entities;
using FluxoCaixa.Lancamentos.Domain.Enums;
using NSubstitute;
using Xunit;

namespace FluxoCaixa.Lancamentos.UnitTests.Application;

public class ObterLancamentosHandlerTests
{
    private readonly ILancamentoRepository _repository = Substitute.For<ILancamentoRepository>();

    private ObterLancamentosHandler CriarHandler() => new(_repository);

    [Fact]
    public async Task Handle_DeveRepassarDataEPaginaParaORepositorioComTamanhoDePaginaFixo()
    {
        _repository
            .ListarAsync(Arg.Any<DateOnly?>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns((Array.Empty<Lancamento>(), 0));

        var data = DateOnly.FromDateTime(DateTime.Today);
        await CriarHandler().Handle(new ObterLancamentosQuery(data, Pagina: 2), CancellationToken.None);

        // Tamanho de página não é recebido do cliente - é sempre o valor fixo do handler.
        await _repository.Received(1).ListarAsync(data, 2, ObterLancamentosHandler.TamanhoPagina, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_DeveMapearItensEMetadadosDePaginacaoNoResultado()
    {
        var lancamento = Lancamento.Criar(DateOnly.FromDateTime(DateTime.Today), TipoLancamento.Credito, 100m, "Venda");
        _repository
            .ListarAsync(Arg.Any<DateOnly?>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns((new[] { lancamento }, 42));

        var resultado = await CriarHandler().Handle(new ObterLancamentosQuery(null, Pagina: 2), CancellationToken.None);

        resultado.Itens.Should().ContainSingle(l => l.Id == lancamento.Id);
        resultado.Pagina.Should().Be(2);
        resultado.TamanhoPagina.Should().Be(ObterLancamentosHandler.TamanhoPagina);
        resultado.TotalItens.Should().Be(42);
        resultado.TotalPaginas.Should().Be(2); // ceil(42 / 30)
    }

    [Fact]
    public async Task Handle_SemResultados_TotalPaginasDeveSerZero()
    {
        _repository
            .ListarAsync(Arg.Any<DateOnly?>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns((Array.Empty<Lancamento>(), 0));

        var resultado = await CriarHandler().Handle(new ObterLancamentosQuery(null), CancellationToken.None);

        resultado.Itens.Should().BeEmpty();
        resultado.TotalPaginas.Should().Be(0);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validator_ComPaginaInvalida_DeveFalhar(int pagina)
    {
        var validator = new ObterLancamentosValidator();

        var resultado = validator.Validate(new ObterLancamentosQuery(null, pagina));

        resultado.IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(1000)]
    public void Validator_ComPaginaValida_DevePassar(int pagina)
    {
        var validator = new ObterLancamentosValidator();

        var resultado = validator.Validate(new ObterLancamentosQuery(null, pagina));

        resultado.IsValid.Should().BeTrue();
    }
}
