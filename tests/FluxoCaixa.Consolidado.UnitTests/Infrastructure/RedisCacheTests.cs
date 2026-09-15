using System.Text.Json;
using FluentAssertions;
using FluxoCaixa.Consolidado.Infrastructure.Cache;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using StackExchange.Redis;
using Xunit;

namespace FluxoCaixa.Consolidado.UnitTests.Infrastructure;

public class RedisCacheTests
{
    private record ValorDeTeste(string Nome, int Quantidade);

    private readonly IConnectionMultiplexer _redis = Substitute.For<IConnectionMultiplexer>();
    private readonly IDatabase _database = Substitute.For<IDatabase>();

    public RedisCacheTests()
    {
        _redis.GetDatabase().Returns(_database);
        _redis.IsConnected.Returns(true);
    }

    private RedisCache CriarCache(int ttlSeconds = 5) =>
        new(_redis, Options.Create(new CacheOptions { TtlSeconds = ttlSeconds }), NullLogger<RedisCache>.Instance);

    [Fact]
    public async Task ObterAsync_ComCacheHit_DeveDesserializarERetornarSemChamarAOrigem()
    {
        var valor = new ValorDeTeste("saldo", 42);
        _database.StringGetAsync(Arg.Any<RedisKey>()).Returns((RedisValue)JsonSerializer.Serialize(valor));

        var origemChamada = false;
        Task<ValorDeTeste?> Origem(CancellationToken _)
        {
            origemChamada = true;
            return Task.FromResult<ValorDeTeste?>(null);
        }

        var resultado = await CriarCache().ObterAsync("chave:teste", Origem, CancellationToken.None);

        resultado.Should().Be(valor);
        origemChamada.Should().BeFalse();
    }

    [Fact]
    public async Task ObterAsync_ComCacheMiss_DeveChamarAOrigemEPopularOCacheComOTtlConfigurado()
    {
        var valor = new ValorDeTeste("saldo", 42);
        _database.StringGetAsync(Arg.Any<RedisKey>()).Returns(RedisValue.Null);

        var resultado = await CriarCache(ttlSeconds: 30).ObterAsync(
            "chave:teste", _ => Task.FromResult<ValorDeTeste?>(valor), CancellationToken.None);

        resultado.Should().Be(valor);
        await _database.Received(1).StringSetAsync(
            (RedisKey)"chave:teste", (RedisValue)JsonSerializer.Serialize(valor), TimeSpan.FromSeconds(30));
    }

    [Fact]
    public async Task ObterAsync_ComCacheMissEOrigemRetornandoNulo_NaoDeveGravarNoCache()
    {
        _database.StringGetAsync(Arg.Any<RedisKey>()).Returns(RedisValue.Null);

        var resultado = await CriarCache().ObterAsync(
            "chave:teste", _ => Task.FromResult<ValorDeTeste?>(null), CancellationToken.None);

        resultado.Should().BeNull();
        await _database.DidNotReceiveWithAnyArgs().StringSetAsync(
            default(RedisKey), default(RedisValue), default(Expiration), default(ValueCondition), default(CommandFlags));
    }

    [Fact]
    public async Task ObterAsync_DeveConsultarAChaveInformadaPeloChamador()
    {
        _database.StringGetAsync(Arg.Any<RedisKey>()).Returns(RedisValue.Null);

        await CriarCache().ObterAsync("consolidado:saldo-diario:2026-03-07", _ => Task.FromResult<ValorDeTeste?>(null), CancellationToken.None);

        await _database.Received(1).StringGetAsync((RedisKey)"consolidado:saldo-diario:2026-03-07");
    }

    [Fact]
    public async Task ObterAsync_DeveRepassarOCancellationTokenParaAOrigem()
    {
        _database.StringGetAsync(Arg.Any<RedisKey>()).Returns(RedisValue.Null);
        using var cts = new CancellationTokenSource();

        CancellationToken? tokenRecebido = null;
        Task<ValorDeTeste?> Origem(CancellationToken ct)
        {
            tokenRecebido = ct;
            return Task.FromResult<ValorDeTeste?>(null);
        }

        await CriarCache().ObterAsync("chave:teste", Origem, cts.Token);

        tokenRecebido.Should().Be(cts.Token);
    }

    [Fact]
    public async Task ObterAsync_ComRedisIndisponivelNaLeitura_DeveCairParaAOrigemEmVezDePropagarAExcecao()
    {
        _database.StringGetAsync(Arg.Any<RedisKey>())
            .Returns<Task<RedisValue>>(_ => throw new RedisConnectionException(ConnectionFailureType.UnableToConnect, "redis fora"));

        var valor = new ValorDeTeste("saldo", 42);

        var resultado = await CriarCache().ObterAsync(
            "chave:teste", _ => Task.FromResult<ValorDeTeste?>(valor), CancellationToken.None);

        resultado.Should().Be(valor);
    }

    [Fact]
    public async Task ObterAsync_ComRedisIndisponivelNaGravacao_DeveDevolverOValorDaOrigemMesmoAssim()
    {
        _database.StringGetAsync(Arg.Any<RedisKey>()).Returns(RedisValue.Null);
        _database.StringSetAsync(Arg.Any<RedisKey>(), Arg.Any<RedisValue>(), Arg.Any<Expiration>())
            .Returns<Task<bool>>(_ => throw new RedisConnectionException(ConnectionFailureType.UnableToConnect, "redis fora"));

        var valor = new ValorDeTeste("saldo", 42);

        var resultado = await CriarCache().ObterAsync(
            "chave:teste", _ => Task.FromResult<ValorDeTeste?>(valor), CancellationToken.None);

        resultado.Should().Be(valor);
    }

    [Fact]
    public async Task ObterAsync_ComConteudoCorrompidoNoCache_DeveCairParaAOrigem()
    {
        _database.StringGetAsync(Arg.Any<RedisKey>()).Returns((RedisValue)"{ isso nao e json valido");

        var valor = new ValorDeTeste("saldo", 42);

        var resultado = await CriarCache().ObterAsync(
            "chave:teste", _ => Task.FromResult<ValorDeTeste?>(valor), CancellationToken.None);

        resultado.Should().Be(valor);
    }

    [Fact]
    public async Task ObterAsync_ComMultiplexerDesconectado_DeveIrDiretoParaAOrigemSemTocarNoRedis()
    {
        _redis.IsConnected.Returns(false);

        var valor = new ValorDeTeste("saldo", 42);

        var resultado = await CriarCache().ObterAsync(
            "chave:teste", _ => Task.FromResult<ValorDeTeste?>(valor), CancellationToken.None);

        resultado.Should().Be(valor);

        // O ponto do curto-circuito é justamente não pagar o SyncTimeout de cada comando
        // enquanto a conexão não volta - nenhuma chamada deve chegar ao Redis.
        await _database.DidNotReceiveWithAnyArgs().StringGetAsync(default(RedisKey));
        await _database.DidNotReceiveWithAnyArgs().StringSetAsync(
            default(RedisKey), default(RedisValue), default(Expiration), default(ValueCondition), default(CommandFlags));
    }
}
