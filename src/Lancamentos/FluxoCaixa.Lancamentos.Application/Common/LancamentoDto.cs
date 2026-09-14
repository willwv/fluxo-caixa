using FluxoCaixa.Lancamentos.Domain.Enums;

namespace FluxoCaixa.Lancamentos.Application.Common;

public record LancamentoDto(Guid Id, DateOnly Data, TipoLancamento Tipo, decimal Valor, string Descricao, DateTime CriadoEm);
