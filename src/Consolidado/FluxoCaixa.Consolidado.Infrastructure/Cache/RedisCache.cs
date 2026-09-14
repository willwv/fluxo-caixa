using System.Text.Json;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace FluxoCaixa.Consolidado.Infrastructure.Cache;

public class RedisCache : IRedisCache
{
    private readonly IConnectionMultiplexer _redis;
    private readonly CacheOptions _options;

    public RedisCache(IConnectionMultiplexer redis, IOptions<CacheOptions> options)
    {
        _redis = redis;
        _options = options.Value;
    }

    public async Task<T?> ObterAsync<T>(string chave, Func<CancellationToken, Task<T?>> obterValorParaCacheAsync, CancellationToken cancellationToken)
    {
        var db = _redis.GetDatabase();

        var valorCacheado = await db.StringGetAsync(chave);
        
        if (valorCacheado.HasValue)
            return JsonSerializer.Deserialize<T>(valorCacheado!);

        var valorAtualizado = await obterValorParaCacheAsync(cancellationToken);

        if (valorAtualizado is not null)
            await db.StringSetAsync(chave, JsonSerializer.Serialize(valorAtualizado), TimeSpan.FromSeconds(_options.TtlSeconds));

        return valorAtualizado;
    }
}
