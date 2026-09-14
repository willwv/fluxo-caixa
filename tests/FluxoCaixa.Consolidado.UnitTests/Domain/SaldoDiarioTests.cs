using FluentAssertions;
using FluxoCaixa.Consolidado.Domain.Entities;
using FluxoCaixa.Consolidado.Domain.Enums;
using Xunit;

namespace FluxoCaixa.Consolidado.UnitTests.Domain;

public class SaldoDiarioTests
{
    [Fact]
    public void Novo_DeveComecarZerado()
    {
        var saldo = SaldoDiario.Novo(DateOnly.FromDateTime(DateTime.Today));

        saldo.TotalCreditos.Should().Be(0m);
        saldo.TotalDebitos.Should().Be(0m);
        saldo.Saldo.Should().Be(0m);
    }

    [Fact]
    public void Aplicar_Credito_DeveSomarEmTotalCreditos()
    {
        var saldo = SaldoDiario.Novo(DateOnly.FromDateTime(DateTime.Today));

        saldo.Aplicar(TipoMovimento.Credito, 100m);
        saldo.Aplicar(TipoMovimento.Credito, 50m);

        saldo.TotalCreditos.Should().Be(150m);
        saldo.Saldo.Should().Be(150m);
    }

    [Fact]
    public void Aplicar_Debito_DeveSomarEmTotalDebitosESubtrairDoSaldo()
    {
        var saldo = SaldoDiario.Novo(DateOnly.FromDateTime(DateTime.Today));

        saldo.Aplicar(TipoMovimento.Credito, 200m);
        saldo.Aplicar(TipoMovimento.Debito, 80m);

        saldo.TotalDebitos.Should().Be(80m);
        saldo.Saldo.Should().Be(120m);
    }

    [Fact]
    public void Aplicar_EmOrdemDiferente_DeveChegarNoMesmoResultado()
    {
        // Comutatividade: a ordem de aplicação dos movimentos não deve afetar o saldo final -
        // é essa propriedade que dispensa garantias de ordenação na consolidação assíncrona.
        var saldoA = SaldoDiario.Novo(DateOnly.FromDateTime(DateTime.Today));
        saldoA.Aplicar(TipoMovimento.Credito, 100m);
        saldoA.Aplicar(TipoMovimento.Debito, 30m);
        saldoA.Aplicar(TipoMovimento.Credito, 20m);

        var saldoB = SaldoDiario.Novo(DateOnly.FromDateTime(DateTime.Today));
        saldoB.Aplicar(TipoMovimento.Credito, 20m);
        saldoB.Aplicar(TipoMovimento.Credito, 100m);
        saldoB.Aplicar(TipoMovimento.Debito, 30m);

        saldoA.Saldo.Should().Be(saldoB.Saldo);
    }

    [Fact]
    public void Aplicar_ComValorZeroOuNegativo_DeveLancarExcecao()
    {
        var saldo = SaldoDiario.Novo(DateOnly.FromDateTime(DateTime.Today));

        var acao = () => saldo.Aplicar(TipoMovimento.Credito, 0m);

        acao.Should().Throw<ArgumentOutOfRangeException>();
    }
}
