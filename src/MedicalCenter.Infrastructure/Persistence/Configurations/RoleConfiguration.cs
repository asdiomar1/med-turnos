using MedicalCenter.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MedicalCenter.Infrastructure.Persistence.Configurations;

public sealed class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("roles");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Code).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(250);
        builder.Property(x => x.Active).HasColumnName("active");
        builder.Property(x => x.IsSystem).HasColumnName("is_system");
        builder.Property(x => x.IsStaff).HasColumnName("is_staff");
        builder.Property(x => x.DefaultHome).HasMaxLength(200).HasColumnName("default_home");
        builder.Property(x => x.Permissions).HasColumnName("permissions").HasColumnType("text[]");
        builder.HasIndex(x => x.Code).IsUnique();
    }
}
