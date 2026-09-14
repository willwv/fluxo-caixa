namespace FluxoCaixa.Consolidado.Infrastructure.Cache;

public class CacheOptions
{
    public const string SectionName = "Redis";

    public string ConnectionString { get; set; } = "localhost:6379";

    // Janela de staleness aceita no relatório: uma leitura pode refletir o saldo com até esse
    // atraso, em troca de tirar carga de leitura do banco sob os 50 req/s de pico.
    public int TtlSeconds { get; set; } = 5;
}
