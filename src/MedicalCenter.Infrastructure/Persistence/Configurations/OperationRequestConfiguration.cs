using MedicalCenter.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MedicalCenter.Infrastructure.Persistence.Configurations;

public sealed class OperationRequestConfiguration : IEntityTypeConfiguration<OperationRequest>
{
    public void Configure(EntityTypeBuilder<OperationRequest> builder)
    {
        builder.ToTable("operation_requests");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Operation).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Key).HasMaxLength(200).IsRequired();
        builder.Property(x => x.RequestHash).HasMaxLength(128).IsRequired();
        builder.Property(x => x.ResponsePayload).HasColumnType("jsonb");
        builder.HasIndex(x => new { x.Operation, x.Key }).IsUnique();
    }
}
