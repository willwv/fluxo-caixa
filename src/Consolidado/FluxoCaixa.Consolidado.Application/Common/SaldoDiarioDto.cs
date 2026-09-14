namespace FluxoCaixa.Consolidado.Application.Common;

public record SaldoDiarioDto(DateOnly Data, decimal TotalCreditos, decimal TotalDebitos, decimal Saldo, DateTime AtualizadoEm);
