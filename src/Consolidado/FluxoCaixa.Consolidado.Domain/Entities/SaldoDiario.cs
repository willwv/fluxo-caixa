using FluxoCaixa.Consolidado.Domain.Enums;

namespace FluxoCaixa.Consolidado.Domain.Entities;

public class SaldoDiario
{
    public DateOnly Data { get; private set; }
    public decimal TotalCreditos { get; private set; }
    public decimal TotalDebitos { get; private set; }
    public decimal Saldo => TotalCreditos - TotalDebitos;
    public DateTime AtualizadoEm { get; private set; }

    private SaldoDiario()
    {
    }

    public static SaldoDiario Novo(DateOnly data) => new()
    {
        Data = data,
        TotalCreditos = 0m,
        TotalDebitos = 0m,
        AtualizadoEm = DateTime.UtcNow
    };

    // Expressa a regra de negócio da consolidação (comutativa: ordem de aplicação não afeta o resultado).
    // A persistência real usa um incremento atômico no banco (UPSERT), não "carregar > somar em memória > salvar",
    // para evitar lost update sob múltiplos consumers concorrentes — ver FluxoCaixa.Consolidado.Infrastructure.
    public void Aplicar(TipoMovimento tipo, decimal valor)
    {
        if (valor <= 0)
            throw new ArgumentOutOfRangeException(nameof(valor), "O valor do movimento deve ser maior que zero.");

        if (tipo == TipoMovimento.Credito)
            TotalCreditos += valor;
        else
            TotalDebitos += valor;

        AtualizadoEm = DateTime.UtcNow;
    }
}
