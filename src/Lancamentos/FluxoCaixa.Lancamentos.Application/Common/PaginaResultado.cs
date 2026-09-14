namespace FluxoCaixa.Lancamentos.Application.Common;

public record PaginaResultado<T>(IReadOnlyList<T> Itens, int Pagina, int TamanhoPagina, int TotalItens)
{
    public int TotalPaginas => TotalItens == 0 ? 0 : (int)Math.Ceiling(TotalItens / (double)TamanhoPagina);
}
