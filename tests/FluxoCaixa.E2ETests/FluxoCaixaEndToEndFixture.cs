extern alias ConsolidadoApi;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;
using Testcontainers.Redis;
using Xunit;
using ConsolidadoProgram = ConsolidadoApi::Program;

namespace FluxoCaixa.E2ETests;

// Sobe a infraestrutura real (Postgres, RabbitMQ, Redis) em containers e hospeda as duas APIs
// in-process (WebApplicationFactory), para validar o fluxo completo: Lançamentos publica um
// evento -> Consolidado consome -> o relatório reflete o novo saldo.
//
// Simplificação deliberada: o banco "réplica" do Consolidado aponta para o MESMO Postgres do
// "primary" neste teste. A topologia de replicação física é uma preocupação de infraestrutura
// (docker-compose), ortogonal à correção do fluxo de negócio que este teste E2E verifica.
public class FluxoCaixaEndToEndFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _lancamentosDb = new PostgreSqlBuilder("postgres:16-alpine")
        .WithDatabase("lancamentos_e2e")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    private readonly PostgreSqlContainer _consolidadoDb = new PostgreSqlBuilder("postgres:16-alpine")
        .WithDatabase("consolidado_e2e")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    // Usuário não-"guest": o RabbitMQ, por padrão, só permite login do usuário guest a partir de
    // localhost - uma conexão vinda de outro container (ou por trás do port-forwarding do Docker
    // Desktop) é recusada com ACCESS_REFUSED. Um usuário customizado não sofre essa restrição.
    private readonly RabbitMqContainer _rabbitMq = new RabbitMqBuilder("rabbitmq:3-management-alpine")
        .WithUsername("testuser")
        .WithPassword("testpass")
        .Build();

    private readonly RedisContainer _redis = new RedisBuilder("redis:7-alpine")
        .Build();

    public const string SigningKey = "chave-de-teste-e2e-com-pelo-menos-32-caracteres-1234";
    public const string SeedUsername = "comerciante-e2e";
    public const string SeedPassword = "SenhaDeTeste!123";

    public WebApplicationFactory<Program> LancamentosFactory { get; private set; } = null!;
    public WebApplicationFactory<ConsolidadoProgram> ConsolidadoFactory { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await Task.WhenAll(
            _lancamentosDb.StartAsync(),
            _consolidadoDb.StartAsync(),
            _rabbitMq.StartAsync(),
            _redis.StartAsync());

        LancamentosFactory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:LancamentosDb"] = _lancamentosDb.GetConnectionString(),
                ["RabbitMq:Host"] = _rabbitMq.Hostname,
                ["RabbitMq:Port"] = _rabbitMq.GetMappedPublicPort(5672).ToString(),
                ["RabbitMq:Username"] = "testuser",
                ["RabbitMq:Password"] = "testpass",
                ["Jwt:SigningKey"] = SigningKey,
                ["PasswordHash:Pepper"] = "pepper-e2e",
                ["SeedUser:Username"] = SeedUsername,
                ["SeedUser:Password"] = SeedPassword
            }));
        });

        ConsolidadoFactory = new WebApplicationFactory<ConsolidadoProgram>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:ConsolidadoPrimaryDb"] = _consolidadoDb.GetConnectionString(),
                ["ConnectionStrings:ConsolidadoReplicaDb"] = _consolidadoDb.GetConnectionString(),
                ["RabbitMq:Host"] = _rabbitMq.Hostname,
                ["RabbitMq:Port"] = _rabbitMq.GetMappedPublicPort(5672).ToString(),
                ["RabbitMq:Username"] = "testuser",
                ["RabbitMq:Password"] = "testpass",
                ["Redis:ConnectionString"] = _redis.GetConnectionString(),
                ["Redis:TtlSeconds"] = "1",
                ["Jwt:SigningKey"] = SigningKey,
                ["Jwt:Issuer"] = "FluxoCaixa.Lancamentos",
                ["Jwt:Audience"] = "FluxoCaixa"
            }));
        });

        // Força a criação dos hosts (e a execução das migrations no startup) antes dos testes.
        _ = LancamentosFactory.Server;
        _ = ConsolidadoFactory.Server;
    }

    public async Task DisposeAsync()
    {
        await LancamentosFactory.DisposeAsync();
        await ConsolidadoFactory.DisposeAsync();
        await _lancamentosDb.DisposeAsync();
        await _consolidadoDb.DisposeAsync();
        await _rabbitMq.DisposeAsync();
        await _redis.DisposeAsync();
    }
}
