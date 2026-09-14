using FluxoCaixa.Lancamentos.Application.Common;
using FluxoCaixa.Lancamentos.Application.Interfaces;
using FluxoCaixa.Lancamentos.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FluxoCaixa.Lancamentos.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly LancamentosDbContext _dbContext;

    public UserRepository(LancamentosDbContext dbContext) => _dbContext = dbContext;

    public async Task<AuthUser?> ObterPorUsernameAsync(string username, CancellationToken cancellationToken)
    {
        var user = await _dbContext.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Username == username, cancellationToken);

        return user is null ? null : new AuthUser(user.Id, user.Username, user.PasswordHash);
    }
}
