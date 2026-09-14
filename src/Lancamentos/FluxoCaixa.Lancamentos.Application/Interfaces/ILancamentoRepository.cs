using FluxoCaixa.Lancamentos.Domain.Entities;

namespace FluxoCaixa.Lancamentos.Application.Interfaces;

public interface ILancamentoRepository
{
    Task AdicionarAsync(Lancamento lancamento, CancellationToken cancellationToken);
    Task<Lancamento?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken);

    Task<(IReadOnlyList<Lancamento> Itens, int TotalItens)> ListarAsync(
        DateOnly? data, int pagina, int tamanhoPagina, CancellationToken cancellationToken);
}
