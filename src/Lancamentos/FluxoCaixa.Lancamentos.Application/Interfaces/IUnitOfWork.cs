namespace FluxoCaixa.Lancamentos.Application.Interfaces;

public interface IUnitOfWork
{
    Task SalvarAlteracoesAsync(CancellationToken cancellationToken);
}
