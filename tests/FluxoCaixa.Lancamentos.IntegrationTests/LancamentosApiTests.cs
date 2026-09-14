using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using FluxoCaixa.Lancamentos.Application.Auth;
using FluxoCaixa.Lancamentos.Application.Common;
using FluxoCaixa.Lancamentos.Application.Lancamentos;
using FluxoCaixa.Lancamentos.Domain.Enums;
using Xunit;

namespace FluxoCaixa.Lancamentos.IntegrationTests;

public class LancamentosApiTests : IClassFixture<LancamentosApiFixture>
{
    private readonly LancamentosApiFixture _fixture;

    public LancamentosApiTests(LancamentosApiFixture fixture) => _fixture = fixture;

    private async Task<string> ObterTokenAsync()
    {
        var client = _fixture.CreateClient();
        var response = await client.PostAsJsonAsync("/auth/login", new { Username = LancamentosApiFixture.SeedUsername, Password = LancamentosApiFixture.SeedPassword });
        response.EnsureSuccessStatusCode();

        var resultado = await response.Content.ReadFromJsonAsync<LoginResultDto>();
        return resultado!.Token;
    }

    [Fact]
    public async Task Login_ComCredenciaisValidas_DeveRetornarToken()
    {
        var client = _fixture.CreateClient();

        var response = await client.PostAsJsonAsync("/auth/login", new { Username = LancamentosApiFixture.SeedUsername, Password = LancamentosApiFixture.SeedPassword });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var resultado = await response.Content.ReadFromJsonAsync<LoginResultDto>();
        resultado!.Token.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Login_ComCredenciaisInvalidas_DeveRetornar401()
    {
        var client = _fixture.CreateClient();

        var response = await client.PostAsJsonAsync("/auth/login", new { Username = "usuario-inexistente", Password = "errada" });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CriarLancamento_SemToken_DeveRetornar401()
    {
        var client = _fixture.CreateClient();

        var response = await client.PostAsJsonAsync("/lancamentos", new CriarLancamentoCommand(DateOnly.FromDateTime(DateTime.Today), TipoLancamento.Credito, 100m, "Venda"));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CriarLancamento_ComTokenValido_DevePersistirERetornar201()
    {
        var client = _fixture.CreateClient();
        var token = await ObterTokenAsync();
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var comando = new CriarLancamentoCommand(DateOnly.FromDateTime(DateTime.Today), TipoLancamento.Credito, 250.50m, "Venda de produto");
        var response = await client.PostAsJsonAsync("/lancamentos", comando);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var criado = await response.Content.ReadFromJsonAsync<LancamentoDto>();
        criado!.Valor.Should().Be(250.50m);

        var consulta = await client.GetAsync($"/lancamentos/{criado.Id}");
        consulta.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CriarLancamento_ComValorInvalido_DeveRetornar400()
    {
        var client = _fixture.CreateClient();
        var token = await ObterTokenAsync();
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var comando = new CriarLancamentoCommand(DateOnly.FromDateTime(DateTime.Today), TipoLancamento.Debito, -10m, "Valor inválido");
        var response = await client.PostAsJsonAsync("/lancamentos", comando);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // Data fixa e exclusiva deste teste para não sofrer interferência dos lançamentos criados
    // (com a data de hoje) pelos outros testes que compartilham o mesmo banco via fixture.
    private static readonly DateOnly DataPaginacao = new(2020, 1, 15);
    private const int QuantidadeLancamentosPaginacao = 3;

    private async Task<HttpClient> ClientePaginacaoComLancamentosCriadosAsync()
    {
        var client = _fixture.CreateClient();
        var token = await ObterTokenAsync();
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        for (var i = 0; i < QuantidadeLancamentosPaginacao; i++)
        {
            var comando = new CriarLancamentoCommand(DataPaginacao, TipoLancamento.Credito, 10m + i, $"Lançamento paginação {i}");
            var criar = await client.PostAsJsonAsync("/lancamentos", comando);
            criar.EnsureSuccessStatusCode();
        }

        return client;
    }

    [Fact]
    public async Task Listar_DeveUsarTamanhoDePaginaFixoDeTrinta()
    {
        var client = await ClientePaginacaoComLancamentosCriadosAsync();

        var pagina = await client.GetFromJsonAsync<PaginaResultado<LancamentoDto>>(
            $"/lancamentos?data={DataPaginacao:yyyy-MM-dd}&pagina=1");

        pagina!.TamanhoPagina.Should().Be(30);
        pagina.Itens.Should().HaveCount(QuantidadeLancamentosPaginacao);
        pagina.TotalItens.Should().Be(QuantidadeLancamentosPaginacao);
        pagina.TotalPaginas.Should().Be(1);
    }

    [Fact]
    public async Task Listar_ClienteNaoConseguePersonalizarOTamanhoDaPagina()
    {
        // O controller não expõe "tamanhoPagina" como parâmetro - passá-lo na query string não
        // deve ter nenhum efeito, o tamanho continua sendo o valor fixo do servidor (30).
        var client = await ClientePaginacaoComLancamentosCriadosAsync();

        var pagina = await client.GetFromJsonAsync<PaginaResultado<LancamentoDto>>(
            $"/lancamentos?data={DataPaginacao:yyyy-MM-dd}&pagina=1&tamanhoPagina=1");

        pagina!.TamanhoPagina.Should().Be(30);
        pagina.Itens.Should().HaveCount(QuantidadeLancamentosPaginacao);
    }

    [Fact]
    public async Task Listar_ComPaginaAlemDoTotal_DeveRetornarVazioMasComMetadadosCorretos()
    {
        var client = await ClientePaginacaoComLancamentosCriadosAsync();

        var pagina = await client.GetFromJsonAsync<PaginaResultado<LancamentoDto>>(
            $"/lancamentos?data={DataPaginacao:yyyy-MM-dd}&pagina=2");

        pagina!.Itens.Should().BeEmpty();
        pagina.TotalItens.Should().Be(QuantidadeLancamentosPaginacao);
        pagina.TotalPaginas.Should().Be(1);
    }

    [Fact]
    public async Task Listar_ComPaginaInvalida_DeveRetornar400()
    {
        var client = _fixture.CreateClient();
        var token = await ObterTokenAsync();
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync($"/lancamentos?data={DataPaginacao:yyyy-MM-dd}&pagina=0");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
