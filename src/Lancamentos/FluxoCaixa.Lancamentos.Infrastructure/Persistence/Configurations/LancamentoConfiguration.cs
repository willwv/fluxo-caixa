using FluxoCaixa.Lancamentos.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FluxoCaixa.Lancamentos.Infrastructure.Persistence.Configurations;

public class LancamentoConfiguration : IEntityTypeConfiguration<Lancamento>
{
    public void Configure(EntityTypeBuilder<Lancamento> builder)
    {
        builder.ToTable("lancamentos");

        builder.HasKey(l => l.Id);

        builder.Property(l => l.Data)
            .HasColumnType("date")
            .IsRequired();

        builder.Property(l => l.Tipo)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(l => l.Valor)
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property(l => l.Descricao)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(l => l.CriadoEm)
            .IsRequired();

        builder.HasIndex(l => l.Data);
    }
}
