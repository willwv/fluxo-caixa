using FluxoCaixa.Consolidado.Application.Common;
using FluxoCaixa.Consolidado.Application.Interfaces;
using MediatR;

namespace FluxoCaixa.Consolidado.Application.SaldoDiario;

public record ObterSaldoDiarioQuery(DateOnly Data) : IRequest<SaldoDiarioDto?>;

public class ObterSaldoDiarioHandler : IRequestHandler<ObterSaldoDiarioQuery, SaldoDiarioDto?>
{
    private readonly ISaldoDiarioReadRepository _repository;

    public ObterSaldoDiarioHandler(ISaldoDiarioReadRepository repository) => _repository = repository;

    public Task<SaldoDiarioDto?> Handle(ObterSaldoDiarioQuery request, CancellationToken cancellationToken) =>
        _repository.ObterAsync(request.Data, cancellationToken);
}
