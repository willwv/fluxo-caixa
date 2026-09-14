using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;
using Xunit;

namespace FluxoCaixa.Lancamentos.IntegrationTests;

// Sobe um Postgres e um RabbitMQ reais em containers (Testcontainers) e configura a API
// (via WebApplicationFactory) para usá-los - testa o pipeline HTTP completo, sem mocks de infra.
public class LancamentosApiFixture : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:16-alpine")
        .WithDatabase("lancamentos_test")
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

    public const string SeedUsername = "comerciante-teste";
    public const string SeedPassword = "SenhaDeTeste!123";

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        await _rabbitMq.StartAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            var overrides = new Dictionary<string, string?>
            {
                ["ConnectionStrings:LancamentosDb"] = _postgres.GetConnectionString(),
                ["RabbitMq:Host"] = _rabbitMq.Hostname,
                ["RabbitMq:Port"] = _rabbitMq.GetMappedPublicPort(5672).ToString(),
                ["RabbitMq:Username"] = "testuser",
                ["RabbitMq:Password"] = "testpass",
                ["Jwt:SigningKey"] = "chave-de-teste-integracao-com-pelo-menos-32-caracteres",
                ["PasswordHash:Pepper"] = "pepper-de-teste",
                ["SeedUser:Username"] = SeedUsername,
                ["SeedUser:Password"] = SeedPassword
            };

            config.AddInMemoryCollection(overrides);
        });
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await _postgres.DisposeAsync();
        await _rabbitMq.DisposeAsync();
        await base.DisposeAsync();
    }
}
