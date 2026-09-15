using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace FluxoCaixa.Consolidado.Infrastructure.Cache;

public class RedisCache : IRedisCache
{
    private readonly IConnectionMultiplexer _redis;
    private readonly CacheOptions _options;
    private readonly ILogger<RedisCache> _logger;

    public RedisCache(IConnectionMultiplexer redis, IOptions<CacheOptions> options, ILogger<RedisCache> logger)
    {
        _redis = redis;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<T?> ObterAsync<T>(string chave, Func<CancellationToken, Task<T?>> obterValorParaCacheAsync, CancellationToken cancellationToken)
    {
        if (!_redis.IsConnected)
        {
            _logger.LogWarning("Redis desconectado; a leitura da chave {Chave} vai direto para a origem.", chave);
            return await obterValorParaCacheAsync(cancellationToken);
        }

        var (encontrado, valorCacheado) = await TentarLerAsync<T>(chave);

        if (encontrado)
            return valorCacheado;

        var valorAtualizado = await obterValorParaCacheAsync(cancellationToken);

        if (valorAtualizado is not null)
            await TentarGravarAsync(chave, valorAtualizado);

        return valorAtualizado;
    }

    private async Task<(bool Encontrado, T? Valor)> TentarLerAsync<T>(string chave)
    {
        try
        {
            var valorCacheado = await _redis.GetDatabase().StringGetAsync(chave);

            return valorCacheado.HasValue
                ? (true, JsonSerializer.Deserialize<T>(valorCacheado!))
                : (false, default);
        }
        catch (Exception ex) when (ex is RedisException or JsonException or ObjectDisposedException)
        {
            _logger.LogWarning(ex, "Falha ao ler a chave {Chave} do Redis; a leitura cai para a origem.", chave);
            return (false, default);
        }
    }

    private async Task TentarGravarAsync<T>(string chave, T valor)
    {
        try
        {
            await _redis.GetDatabase().StringSetAsync(chave, JsonSerializer.Serialize(valor), TimeSpan.FromSeconds(_options.TtlSeconds));
        }
        catch (Exception ex) when (ex is RedisException or JsonException or ObjectDisposedException)
        {
            _logger.LogWarning(ex, "Falha ao gravar a chave {Chave} no Redis; o valor da origem é devolvido mesmo assim.", chave);
        }
    }
}
