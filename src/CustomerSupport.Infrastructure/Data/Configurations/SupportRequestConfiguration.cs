using CustomerSupport.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CustomerSupport.Infrastructure.Data.Configurations;

public class SupportRequestConfiguration : IEntityTypeConfiguration<SupportRequest>
{
    public void Configure(EntityTypeBuilder<SupportRequest> builder)
    {
        builder.ToTable("SupportRequests");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Subject)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(r => r.Description)
            .IsRequired()
            .HasMaxLength(4000);

        builder.Property(r => r.Status)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(r => r.Priority)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(r => r.CreatedAt)
            .IsRequired();

        // Concurrency token mapped to PostgreSQL 'xmin' system column
        builder.Property(r => r.RowVersion)
            .IsRowVersion();

        // Foreign Key: Requester
        builder.HasOne(r => r.RequesterUser)
            .WithMany(u => u.SubmittedRequests)
            .HasForeignKey(r => r.RequesterUserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Foreign Key: Assigned Agent (optional)
        builder.HasOne(r => r.AssignedAgentUser)
            .WithMany(u => u.AssignedRequests)
            .HasForeignKey(r => r.AssignedAgentUserId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes for performance
        builder.HasIndex(r => r.Status);
        builder.HasIndex(r => r.RequesterUserId);
        builder.HasIndex(r => r.AssignedAgentUserId);
        builder.HasIndex(r => r.CreatedAt);
    }
}
