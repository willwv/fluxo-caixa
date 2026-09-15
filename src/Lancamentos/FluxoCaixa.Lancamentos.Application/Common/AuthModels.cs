namespace FluxoCaixa.Lancamentos.Application.Common;

// Representação enxuta do usuário exposta pela porta IUserRepository -
// evita que a camada de Application dependa da entidade EF de Infrastructure.
public record AuthUser(Guid Id, string Username, string PasswordHash);

public record LoginResultDto(string Token, DateTime ExpiraEm);

public record TokenGerado(string Token, DateTime ExpiraEm);
