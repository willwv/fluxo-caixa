using FluxoCaixa.Lancamentos.Application.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace FluxoCaixa.Lancamentos.Infrastructure.Persistence;

// Semeia o único usuário (comerciante) do sistema, se ainda não existir.
// Não há endpoint de cadastro de usuário: o domínio não pede multiusuário, então evitamos
// construir essa feature - ver ADR de autenticação.
public static class DbSeeder
{
    public static async Task SeedAsync(LancamentosDbContext dbContext, IConfiguration configuration, IPasswordHasher passwordHasher)
    {
        await dbContext.Database.MigrateAsync();

        var jaExiste = await dbContext.Users.AnyAsync();
        if (jaExiste)
            return;

        var username = configuration["SeedUser:Username"] ?? "comerciante";
        var password = configuration["SeedUser:Password"] ?? "TrocarEssaSenha!123";

        var user = UserAccount.Criar(username, passwordHasher.Hash(password));
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();
    }
}
