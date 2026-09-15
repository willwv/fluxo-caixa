using FluxoCaixa.Lancamentos.Application.Common;

namespace FluxoCaixa.Lancamentos.Application.Interfaces;

public interface IUserRepository
{
    Task<AuthUser?> ObterPorUsernameAsync(string username, CancellationToken cancellationToken);
}

// BCrypt cuida do salt (único por usuário, embutido no hash). O "pepper" é um segredo único da
// aplicação, vindo de configuração/secrets - nunca do banco - concatenado antes do hash.
public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string passwordHash);
}

public interface IJwtTokenGenerator
{
    TokenGerado GerarToken(Guid userId, string username);
}
