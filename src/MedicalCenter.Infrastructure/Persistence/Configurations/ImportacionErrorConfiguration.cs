using MedicalCenter.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MedicalCenter.Infrastructure.Persistence.Configurations;

public sealed class ImportacionErrorConfiguration : IEntityTypeConfiguration<ImportacionError>
{
    public void Configure(EntityTypeBuilder<ImportacionError> builder)
    {
        builder.ToTable("importacion_errores");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedOnAdd();
        builder.Property(x => x.ImportacionId).HasColumnName("importacion_id").IsRequired();
        builder.Property(x => x.RowNumber).HasColumnName("row_number").IsRequired();
        builder.Property(x => x.Field).HasColumnName("field").HasMaxLength(100);
        builder.Property(x => x.Message).HasColumnName("message").IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired();

        builder.HasIndex(x => new { x.ImportacionId, x.RowNumber }).HasDatabaseName("ix_importacion_errores_importacion");
    }
}
