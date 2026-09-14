namespace FluxoCaixa.Consolidado.Infrastructure.Cache;

// Cache read-through genérico: cada chamador só informa a chave e como obter o valor na origem
// em caso de miss - quem decide TTL, serialização e a própria interação com o Redis é esta
// classe, não o repositório que a consome.
public interface IRedisCache
{
    Task<T?> ObterAsync<T>(string chave, Func<CancellationToken, Task<T?>> obterDaOrigemAsync, CancellationToken cancellationToken);
}
