using FluxoCaixa.Lancamentos.Application.Common;
using FluxoCaixa.Lancamentos.Application.Interfaces;
using MediatR;

namespace FluxoCaixa.Lancamentos.Application.Lancamentos;

public record ObterLancamentoPorIdQuery(Guid Id) : IRequest<LancamentoDto?>;

public class ObterLancamentoPorIdHandler : IRequestHandler<ObterLancamentoPorIdQuery, LancamentoDto?>
{
    private readonly ILancamentoRepository _lancamentosRepository;

    public ObterLancamentoPorIdHandler(ILancamentoRepository repository) => _lancamentosRepository = repository;

    public async Task<LancamentoDto?> Handle(ObterLancamentoPorIdQuery request, CancellationToken cancellationToken)
    {
        var lancamento = await _lancamentosRepository.ObterPorIdAsync(request.Id, cancellationToken);
        return lancamento is null
            ? null
            : new LancamentoDto(lancamento.Id, lancamento.Data, lancamento.Tipo, lancamento.Valor, lancamento.Descricao, lancamento.CriadoEm);
    }
}
