using MedicalCenter.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MedicalCenter.Infrastructure.Persistence.Configurations;

public sealed class ImportacionConfiguration : IEntityTypeConfiguration<Importacion>
{
    public void Configure(EntityTypeBuilder<Importacion> builder)
    {
        builder.ToTable("importaciones");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(x => x.Tipo).HasColumnName("tipo").HasMaxLength(50).IsRequired();
        builder.Property(x => x.Estado).HasColumnName("estado").HasMaxLength(30).IsRequired();
        builder.Property(x => x.UsuarioId).HasColumnName("usuario_id").IsRequired();
        builder.Property(x => x.FileName).HasColumnName("file_name").HasMaxLength(255).IsRequired();
        builder.Property(x => x.StorageProvider).HasColumnName("storage_provider").HasMaxLength(20).IsRequired();
        builder.Property(x => x.StorageBucket).HasColumnName("storage_bucket").HasMaxLength(100).IsRequired();
        builder.Property(x => x.StorageKey).HasColumnName("storage_key").HasMaxLength(500).IsRequired();
        builder.Property(x => x.SizeBytes).HasColumnName("size_bytes").IsRequired();
        builder.Property(x => x.ContentType).HasColumnName("content_type").HasMaxLength(200).IsRequired();
        builder.Property(x => x.Sha256).HasColumnName("sha256").HasMaxLength(64);
        builder.Property(x => x.TotalFilas).HasColumnName("total_filas").IsRequired();
        builder.Property(x => x.FilasValidas).HasColumnName("filas_validas").IsRequired();
        builder.Property(x => x.FilasConError).HasColumnName("filas_con_error").IsRequired();
        builder.Property(x => x.FilasInsertadas).HasColumnName("filas_insertadas").IsRequired();
        builder.Property(x => x.FilasActualizadas).HasColumnName("filas_actualizadas").IsRequired();
        builder.Property(x => x.ErrorMessage).HasColumnName("error_message");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(x => x.StartedAt).HasColumnName("started_at");
        builder.Property(x => x.FinishedAt).HasColumnName("finished_at");
        builder.Property(x => x.ExpiresAt).HasColumnName("expires_at");

        builder.HasIndex(x => new { x.UsuarioId, x.CreatedAt }).HasDatabaseName("ix_importaciones_usuario");
        builder.HasIndex(x => x.StorageKey).IsUnique().HasDatabaseName("ux_importaciones_storage_key");

        builder.HasMany<ImportacionError>()
            .WithOne()
            .HasForeignKey(x => x.ImportacionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
