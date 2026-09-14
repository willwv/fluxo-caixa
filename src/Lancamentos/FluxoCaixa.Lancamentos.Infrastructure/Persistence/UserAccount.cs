namespace FluxoCaixa.Lancamentos.Infrastructure.Persistence;

public class UserAccount
{
    public Guid Id { get; private set; }
    public string Username { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public DateTime CriadoEm { get; private set; }

    private UserAccount()
    {
    }

    public static UserAccount Criar(string username, string passwordHash) => new()
    {
        Id = Guid.NewGuid(),
        Username = username,
        PasswordHash = passwordHash,
        CriadoEm = DateTime.UtcNow
    };
}
