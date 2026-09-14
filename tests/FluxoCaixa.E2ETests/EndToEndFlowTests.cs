using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using FluxoCaixa.Contracts;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace FluxoCaixa.E2ETests;

public class EndToEndFlowTests : IClassFixture<FluxoCaixaEndToEndFixture>
{
    private readonly FluxoCaixaEndToEndFixture _fixture;

    public EndToEndFlowTests(FluxoCaixaEndToEndFixture fixture) => _fixture = fixture;

    private record LoginResponse(string Token, DateTime ExpiraEm);

    private record SaldoDiarioResponse(DateOnly Data, decimal TotalCreditos, decimal TotalDebitos, decimal Saldo, DateTime AtualizadoEm);

    [Fact]
    public async Task FluxoCompleto_LancamentoPublicadoEConsumido_DeveAtualizarSaldoConsolidado()
    {
        var lancamentosClient = _fixture.LancamentosFactory.CreateClient();
        var consolidadoClient = _fixture.ConsolidadoFactory.CreateClient();

        var loginResponse = await lancamentosClient.PostAsJsonAsync("/auth/login",
            new { Username = FluxoCaixaEndToEndFixture.SeedUsername, Password = FluxoCaixaEndToEndFixture.SeedPassword });
        loginResponse.EnsureSuccessStatusCode();
        var login = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();

        lancamentosClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login!.Token);
        consolidadoClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.Token);

        var data = DateOnly.FromDateTime(DateTime.UtcNow);

        var criarCredito = await lancamentosClient.PostAsJsonAsync("/lancamentos", new { Data = data, Tipo = 1, Valor = 300m, Descricao = "Venda E2E" });
        criarCredito.EnsureSuccessStatusCode();

        var criarDebito = await lancamentosClient.PostAsJsonAsync("/lancamentos", new { Data = data, Tipo = 2, Valor = 120m, Descricao = "Despesa E2E" });
        criarDebito.EnsureSuccessStatusCode();

        // O consumo do evento é assíncrono (outbox -> RabbitMQ -> consumer) - a consolidação é
        // eventualmente consistente, então o teste faz polling em vez de assumir uma janela fixa.
        var saldo = await AguardarSaldoAsync(consolidadoClient, data, esperado: 180m, tentativas: 30, intervalo: TimeSpan.FromSeconds(1));

        saldo.Should().NotBeNull();
        saldo!.TotalCreditos.Should().Be(300m);
        saldo.TotalDebitos.Should().Be(120m);
        saldo.Saldo.Should().Be(180m);
    }

    [Fact]
    public async Task EventoDuplicado_MesmoEventId_NaoDeveContarDuasVezes()
    {
        var lancamentosClient = _fixture.LancamentosFactory.CreateClient();
        var consolidadoClient = _fixture.ConsolidadoFactory.CreateClient();

        var loginResponse = await lancamentosClient.PostAsJsonAsync("/auth/login",
            new { Username = FluxoCaixaEndToEndFixture.SeedUsername, Password = FluxoCaixaEndToEndFixture.SeedPassword });
        var login = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();
        consolidadoClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login!.Token);

        var data = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-1);
        var evento = new LancamentoRegistrado(Guid.NewGuid(), Guid.NewGuid(), data, TipoLancamentoContrato.Credito, 500m, DateTime.UtcNow);

        // Publica o MESMO evento (mesmo EventId) duas vezes, simulando uma entrega duplicada do
        // broker - a idempotência do consumer (ver ADR 0007) deve garantir que só é aplicado uma vez.
        using var scope = _fixture.ConsolidadoFactory.Services.CreateScope();
        var publishEndpoint = scope.ServiceProvider.GetRequiredService<IPublishEndpoint>();
        await publishEndpoint.Publish(evento);
        await publishEndpoint.Publish(evento);

        var saldo = await AguardarSaldoAsync(consolidadoClient, data, esperado: 500m, tentativas: 15, intervalo: TimeSpan.FromSeconds(1));
        saldo.Should().NotBeNull();

        // Espera mais um pouco e reconfere: garante que a segunda entrega não aplicou o
        // incremento de forma atrasada (o polling acima para assim que vê 500, o que sozinho
        // não provaria que os 500 não viram 1000 logo em seguida).
        await Task.Delay(TimeSpan.FromSeconds(3));
        var response = await consolidadoClient.GetAsync($"/consolidado/{data:yyyy-MM-dd}");
        var saldoFinal = await response.Content.ReadFromJsonAsync<SaldoDiarioResponse>();

        saldoFinal!.TotalCreditos.Should().Be(500m, "o evento duplicado não deveria ter sido contado de novo");
    }

    private static async Task<SaldoDiarioResponse?> AguardarSaldoAsync(HttpClient client, DateOnly data, decimal esperado, int tentativas, TimeSpan intervalo)
    {
        for (var i = 0; i < tentativas; i++)
        {
            var response = await client.GetAsync($"/consolidado/{data:yyyy-MM-dd}");
            if (response.IsSuccessStatusCode)
            {
                var saldo = await response.Content.ReadFromJsonAsync<SaldoDiarioResponse>();
                if (saldo is not null && saldo.Saldo == esperado)
                    return saldo;
            }

            await Task.Delay(intervalo);
        }

        return null;
    }
}
