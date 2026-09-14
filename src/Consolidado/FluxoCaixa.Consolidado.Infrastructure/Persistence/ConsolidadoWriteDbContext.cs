using FluxoCaixa.Consolidado.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FluxoCaixa.Consolidado.Infrastructure.Persistence;

// Aponta para o banco PRIMARY. Usado pelo consumer para aplicar lançamentos (via SQL atômico)
// e é o único DbContext contra o qual migrations são executadas - a réplica recebe o schema
// via replicação física do Postgres, não via `dotnet ef database update`.
public class ConsolidadoWriteDbContext : DbContext
{
    public ConsolidadoWriteDbContext(DbContextOptions<ConsolidadoWriteDbContext> options) : base(options)
    {
    }

    public DbSet<SaldoDiario> SaldosDiarios => Set<SaldoDiario>();
    public DbSet<ProcessedEvent> ProcessedEvents => Set<ProcessedEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ConsolidadoWriteDbContext).Assembly);
    }
}
