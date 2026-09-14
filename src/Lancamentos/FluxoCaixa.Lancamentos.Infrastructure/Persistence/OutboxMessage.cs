namespace FluxoCaixa.Lancamentos.Infrastructure.Persistence;

public class OutboxMessage
{
    public Guid Id { get; private set; }
    public string Tipo { get; private set; } = string.Empty;
    public string Conteudo { get; private set; } = string.Empty;
    public DateTime OcorridoEm { get; private set; }
    public DateTime? ProcessadoEm { get; private set; }
    public int Tentativas { get; private set; }
    public string? UltimoErro { get; private set; }

    private OutboxMessage()
    {
    }

    public static OutboxMessage Criar(string tipo, string conteudoJson) => new()
    {
        Id = Guid.NewGuid(),
        Tipo = tipo,
        Conteudo = conteudoJson,
        OcorridoEm = DateTime.UtcNow
    };

    public void MarcarProcessado() => ProcessadoEm = DateTime.UtcNow;

    public void RegistrarFalha(string erro)
    {
        Tentativas++;
        UltimoErro = erro;
    }
}
