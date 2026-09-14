using FluxoCaixa.Consolidado.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FluxoCaixa.Consolidado.Infrastructure.Persistence.Configurations;

public class SaldoDiarioConfiguration : IEntityTypeConfiguration<SaldoDiario>
{
    public void Configure(EntityTypeBuilder<SaldoDiario> builder)
    {
        builder.ToTable("saldo_diario");

        builder.HasKey(s => s.Data);

        builder.Property(s => s.Data).HasColumnType("date");
        builder.Property(s => s.TotalCreditos).HasColumnType("decimal(18,2)");
        builder.Property(s => s.TotalDebitos).HasColumnType("decimal(18,2)");
        builder.Property(s => s.AtualizadoEm).IsRequired();

        // Saldo é calculado em memória (TotalCreditos - TotalDebitos), não é uma coluna persistida.
        builder.Ignore(s => s.Saldo);
    }
}
