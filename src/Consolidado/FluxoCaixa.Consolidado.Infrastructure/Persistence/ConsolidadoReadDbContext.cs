using FluxoCaixa.Consolidado.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FluxoCaixa.Consolidado.Infrastructure.Persistence;

// Aponta para a READ REPLICA. Somente leitura - nunca é alvo de SaveChanges/migrations.
public class ConsolidadoReadDbContext : DbContext
{
    public ConsolidadoReadDbContext(DbContextOptions<ConsolidadoReadDbContext> options) : base(options)
    {
    }

    public DbSet<SaldoDiario> SaldosDiarios => Set<SaldoDiario>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new Configurations.SaldoDiarioConfiguration());
    }
}
