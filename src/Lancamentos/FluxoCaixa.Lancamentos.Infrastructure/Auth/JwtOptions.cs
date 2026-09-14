namespace FluxoCaixa.Lancamentos.Infrastructure.Auth;

public class JwtOptions
{
    public const string SectionName = "Jwt";

    public string SigningKey { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public int ExpirationHours { get; set; } = 1;
}

public class PasswordHashOptions
{
    public const string SectionName = "PasswordHash";

    // Segredo global da aplicação (pepper), somado ao salt já embutido pelo BCrypt em cada hash.
    // Vem de configuração/.env local; em produção viria de um vault (Key Vault/Secrets Manager).
    public string Pepper { get; set; } = string.Empty;
}
