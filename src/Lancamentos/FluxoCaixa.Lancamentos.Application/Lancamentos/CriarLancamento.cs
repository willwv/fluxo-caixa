using FluentValidation;
using FluxoCaixa.Contracts;
using FluxoCaixa.Lancamentos.Application.Common;
using FluxoCaixa.Lancamentos.Application.Interfaces;
using FluxoCaixa.Lancamentos.Domain.Entities;
using FluxoCaixa.Lancamentos.Domain.Enums;
using MediatR;

namespace FluxoCaixa.Lancamentos.Application.Lancamentos;

public record CriarLancamentoCommand(DateOnly Data, TipoLancamento Tipo, decimal Valor, string Descricao)
    : IRequest<LancamentoDto>;

public class CriarLancamentoValidator : AbstractValidator<CriarLancamentoCommand>
{
    public CriarLancamentoValidator()
    {
        RuleFor(x => x.Valor).GreaterThan(0m).WithMessage("O valor deve ser maior que zero.");
        RuleFor(x => x.Descricao).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Tipo).IsInEnum();
    }
}

public class CriarLancamentoHandler : IRequestHandler<CriarLancamentoCommand, LancamentoDto>
{
    private readonly ILancamentoRepository _lancamentoRepository;
    private readonly IOutboxWriter _outboxWriter;
    private readonly IUnitOfWork _unitOfWork;

    public CriarLancamentoHandler(ILancamentoRepository repository, IOutboxWriter outboxWriter, IUnitOfWork unitOfWork)
    {
        _lancamentoRepository = repository;
        _outboxWriter = outboxWriter;
        _unitOfWork = unitOfWork;
    }

    public async Task<LancamentoDto> Handle(CriarLancamentoCommand request, CancellationToken cancellationToken)
    {
        var lancamento = Lancamento.Criar(request.Data, request.Tipo, request.Valor, request.Descricao);

        await _lancamentoRepository.AdicionarAsync(lancamento, cancellationToken);

        var evento = new LancamentoRegistrado(
            EventId: Guid.NewGuid(),
            LancamentoId: lancamento.Id,
            Data: lancamento.Data,
            Tipo: lancamento.Tipo == TipoLancamento.Credito ? TipoLancamentoContrato.Credito : TipoLancamentoContrato.Debito,
            Valor: lancamento.Valor,
            OcorreuEm: DateTime.UtcNow);

        await _outboxWriter.AdicionarAsync(evento, cancellationToken);

        // Lançamento + evento na tabela outbox são persistidos juntos, na mesma transação do EF Core.
        await _unitOfWork.SalvarAlteracoesAsync(cancellationToken);

        return new LancamentoDto(lancamento.Id, lancamento.Data, lancamento.Tipo, lancamento.Valor, lancamento.Descricao, lancamento.CriadoEm);
    }
}
