using FluentValidation;
using FluxoCaixa.Lancamentos.Application.Common;
using FluxoCaixa.Lancamentos.Application.Interfaces;
using MediatR;

namespace FluxoCaixa.Lancamentos.Application.Lancamentos;

public record ObterLancamentosQuery(DateOnly? Data, int Pagina = 1) : IRequest<PaginaResultado<LancamentoDto>>;

public class ObterLancamentosValidator : AbstractValidator<ObterLancamentosQuery>
{
    public ObterLancamentosValidator()
    {
        RuleFor(x => x.Pagina).GreaterThanOrEqualTo(1).WithMessage("A página deve ser maior ou igual a 1.");
    }
}

public class ObterLancamentosHandler : IRequestHandler<ObterLancamentosQuery, PaginaResultado<LancamentoDto>>
{
    public const int TamanhoPagina = 30;

    private readonly ILancamentoRepository _lancamentoRepository;

    public ObterLancamentosHandler(ILancamentoRepository repository) => _lancamentoRepository = repository;

    public async Task<PaginaResultado<LancamentoDto>> Handle(ObterLancamentosQuery request, CancellationToken cancellationToken)
    {
        var (lancamentos, totalItens) = await _lancamentoRepository.ListarAsync(
            request.Data, request.Pagina, TamanhoPagina, cancellationToken);

        var itens = lancamentos
            .Select(l => new LancamentoDto(l.Id, l.Data, l.Tipo, l.Valor, l.Descricao, l.CriadoEm))
            .ToList();

        return new PaginaResultado<LancamentoDto>(itens, request.Pagina, TamanhoPagina, totalItens);
    }
}
