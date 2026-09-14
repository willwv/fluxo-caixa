using FluxoCaixa.Lancamentos.Application.Interfaces;
using Microsoft.Extensions.Options;

namespace FluxoCaixa.Lancamentos.Infrastructure.Auth;

public class BCryptPasswordHasher : IPasswordHasher
{
    private readonly string _pepper;

    public BCryptPasswordHasher(IOptions<PasswordHashOptions> options) => _pepper = options.Value.Pepper;

    public string Hash(string password) => BCrypt.Net.BCrypt.HashPassword(ComPepper(password));

    public bool Verify(string password, string passwordHash) => BCrypt.Net.BCrypt.Verify(ComPepper(password), passwordHash);

    private string ComPepper(string password) => password + _pepper;
}
