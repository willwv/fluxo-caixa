using FluentAssertions;
using FluxoCaixa.Lancamentos.Domain.Entities;
using FluxoCaixa.Lancamentos.Domain.Enums;
using FluxoCaixa.Lancamentos.Domain.Exceptions;
using Xunit;

namespace FluxoCaixa.Lancamentos.UnitTests.Domain;

public class LancamentoTests
{
    [Fact]
    public void Criar_ComDadosValidos_DeveCriarLancamento()
    {
        var data = new DateOnly(2026, 9, 12);

        var lancamento = Lancamento.Criar(data, TipoLancamento.Credito, 150.75m, "Venda de produto");

        lancamento.Id.Should().NotBeEmpty();
        lancamento.Data.Should().Be(data);
        lancamento.Tipo.Should().Be(TipoLancamento.Credito);
        lancamento.Valor.Should().Be(150.75m);
        lancamento.Descricao.Should().Be("Venda de produto");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public void Criar_ComValorMenorOuIgualAZero_DeveLancarExcecao(decimal valor)
    {
        var acao = () => Lancamento.Criar(DateOnly.FromDateTime(DateTime.Today), TipoLancamento.Debito, valor, "Despesa");

        acao.Should().Throw<DomainException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Criar_ComDescricaoVazia_DeveLancarExcecao(string? descricao)
    {
        var acao = () => Lancamento.Criar(DateOnly.FromDateTime(DateTime.Today), TipoLancamento.Debito, 10m, descricao!);

        acao.Should().Throw<DomainException>();
    }

    [Fact]
    public void ValorComSinal_ParaCredito_DeveSerPositivo()
    {
        var lancamento = Lancamento.Criar(DateOnly.FromDateTime(DateTime.Today), TipoLancamento.Credito, 100m, "Venda");

        lancamento.ValorComSinal().Should().Be(100m);
    }

    [Fact]
    public void ValorComSinal_ParaDebito_DeveSerNegativo()
    {
        var lancamento = Lancamento.Criar(DateOnly.FromDateTime(DateTime.Today), TipoLancamento.Debito, 100m, "Despesa");

        lancamento.ValorComSinal().Should().Be(-100m);
    }
}
