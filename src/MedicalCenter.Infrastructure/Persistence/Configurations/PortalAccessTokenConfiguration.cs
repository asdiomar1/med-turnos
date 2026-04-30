using MedicalCenter.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MedicalCenter.Infrastructure.Persistence.Configurations;

public sealed class PortalAccessTokenConfiguration : IEntityTypeConfiguration<PortalAccessToken>
{
    public void Configure(EntityTypeBuilder<PortalAccessToken> builder)
    {
        builder.ToTable("portal_access_tokens");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Purpose).HasMaxLength(20).IsRequired();
        builder.Property(x => x.DeliveryChannel).HasMaxLength(20).IsRequired();
        builder.Property(x => x.TokenHash).HasMaxLength(200).IsRequired();
        builder.Property(x => x.IssuedToMasked).HasMaxLength(100);
        builder.Property(x => x.LastAttemptAt);
        builder.Property(x => x.Metadata).HasColumnType("jsonb").HasDefaultValueSql("'{}'::jsonb");
        builder.HasIndex(x => x.TokenHash).IsUnique().HasFilter("\"UsedAt\" IS NULL AND \"RevokedAt\" IS NULL");
        builder.HasIndex(x => new { x.PacienteId, x.Purpose });
    }
}
