using FluentAssertions;
using FluxoCaixa.Contracts;
using FluxoCaixa.Lancamentos.Application.Interfaces;
using FluxoCaixa.Lancamentos.Application.Lancamentos;
using FluxoCaixa.Lancamentos.Domain.Entities;
using FluxoCaixa.Lancamentos.Domain.Enums;
using NSubstitute;
using Xunit;

namespace FluxoCaixa.Lancamentos.UnitTests.Application;

public class CriarLancamentoHandlerTests
{
    private readonly ILancamentoRepository _repository = Substitute.For<ILancamentoRepository>();
    private readonly IOutboxWriter _outboxWriter = Substitute.For<IOutboxWriter>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private CriarLancamentoHandler CriarHandler() => new(_repository, _outboxWriter, _unitOfWork);

    [Fact]
    public async Task Handle_ComComandoValido_DevePersistirEEnfileirarEvento()
    {
        var command = new CriarLancamentoCommand(DateOnly.FromDateTime(DateTime.Today), TipoLancamento.Credito, 200m, "Venda à vista");

        var resultado = await CriarHandler().Handle(command, CancellationToken.None);

        resultado.Valor.Should().Be(200m);
        resultado.Tipo.Should().Be(TipoLancamento.Credito);

        await _repository.Received(1).AdicionarAsync(Arg.Is<Lancamento>(l => l.Valor == 200m), Arg.Any<CancellationToken>());
        await _outboxWriter.Received(1).AdicionarAsync(
            Arg.Is<LancamentoRegistrado>(e => e.Valor == 200m && e.Tipo == TipoLancamentoContrato.Credito),
            Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SalvarAlteracoesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ComDebito_DeveMapearTipoCorretamenteNoEvento()
    {
        var command = new CriarLancamentoCommand(DateOnly.FromDateTime(DateTime.Today), TipoLancamento.Debito, 50m, "Pagamento de fornecedor");

        await CriarHandler().Handle(command, CancellationToken.None);

        await _outboxWriter.Received(1).AdicionarAsync(
            Arg.Is<LancamentoRegistrado>(e => e.Tipo == TipoLancamentoContrato.Debito),
            Arg.Any<CancellationToken>());
    }
}
