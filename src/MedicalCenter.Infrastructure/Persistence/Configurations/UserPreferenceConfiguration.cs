using MedicalCenter.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MedicalCenter.Infrastructure.Persistence.Configurations;

public sealed class UserPreferenceConfiguration : IEntityTypeConfiguration<UserPreference>
{
    public void Configure(EntityTypeBuilder<UserPreference> builder)
    {
        builder.ToTable("user_preferences");
        builder.HasKey(x => x.UserId);
        builder.Property(x => x.Theme).HasMaxLength(50).IsRequired();
        builder.Property(x => x.CustomColorsJson).HasColumnType("jsonb");
        builder.Property(x => x.TurnosLayout).HasMaxLength(50).IsRequired();
        builder.Property(x => x.FontScale).HasColumnType("numeric(5,2)");
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired();
        builder.HasOne<User>().WithOne().HasForeignKey<UserPreference>(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}
