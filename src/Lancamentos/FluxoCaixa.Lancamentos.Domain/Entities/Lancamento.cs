using FluxoCaixa.Lancamentos.Domain.Enums;
using FluxoCaixa.Lancamentos.Domain.Exceptions;

namespace FluxoCaixa.Lancamentos.Domain.Entities;

public class Lancamento
{
    public Guid Id { get; private set; }
    public DateOnly Data { get; private set; }
    public TipoLancamento Tipo { get; private set; }
    public decimal Valor { get; private set; }
    public string Descricao { get; private set; } = string.Empty;
    public DateTime CriadoEm { get; private set; }

    private Lancamento()
    {
    }

    private Lancamento(Guid id, DateOnly data, TipoLancamento tipo, decimal valor, string descricao, DateTime criadoEm)
    {
        Id = id;
        Data = data;
        Tipo = tipo;
        Valor = valor;
        Descricao = descricao;
        CriadoEm = criadoEm;
    }

    public static Lancamento Criar(DateOnly data, TipoLancamento tipo, decimal valor, string descricao)
    {
        if (valor <= 0)
            throw new DomainException("O valor do lançamento deve ser maior que zero.");

        if (string.IsNullOrWhiteSpace(descricao))
            throw new DomainException("A descrição do lançamento é obrigatória.");

        if (descricao.Length > 200)
            throw new DomainException("A descrição do lançamento deve ter no máximo 200 caracteres.");

        return new Lancamento(Guid.NewGuid(), data, tipo, valor, descricao.Trim(), DateTime.UtcNow);
    }

    // Convenção de sinal usada na consolidação do saldo: crédito soma, débito subtrai.
    public decimal ValorComSinal() => Tipo == TipoLancamento.Credito ? Valor : -Valor;
}
