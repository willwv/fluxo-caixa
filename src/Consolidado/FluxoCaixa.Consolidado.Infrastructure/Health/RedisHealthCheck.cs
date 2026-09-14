using Microsoft.Extensions.Diagnostics.HealthChecks;
using StackExchange.Redis;

namespace FluxoCaixa.Consolidado.Infrastructure.Health;

public class RedisHealthCheck : IHealthCheck
{
    private readonly IConnectionMultiplexer _redis;

    public RedisHealthCheck(IConnectionMultiplexer redis) => _redis = redis;

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var latency = await _redis.GetDatabase().PingAsync();
            return HealthCheckResult.Healthy($"Redis respondeu em {latency.TotalMilliseconds}ms.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Não foi possível conectar ao Redis.", ex);
        }
    }
}
