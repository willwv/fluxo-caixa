using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FluxoCaixa.Lancamentos.Infrastructure.Persistence.Configurations;

public class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("outbox_messages");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.Tipo).HasMaxLength(200).IsRequired();
        builder.Property(m => m.Conteudo).HasColumnType("jsonb").IsRequired();
        builder.Property(m => m.OcorridoEm).IsRequired();
        builder.Property(m => m.UltimoErro).HasMaxLength(2000);

        // Índice parcial: o publisher só varre mensagens pendentes, mantendo a consulta barata
        // mesmo com a tabela crescendo (mensagens já processadas não entram no índice).
        builder.HasIndex(m => m.OcorridoEm)
            .HasFilter("\"processado_em\" IS NULL")
            .HasDatabaseName("ix_outbox_messages_pendentes");
    }
}
