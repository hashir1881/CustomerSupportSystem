using CustomerSupport.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CustomerSupport.Infrastructure.Data.Configurations;

public class ConversationConfiguration : IEntityTypeConfiguration<Conversation>
{
    public void Configure(EntityTypeBuilder<Conversation> builder)
    {
        builder.ToTable("Conversations");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.CreatedAt)
            .IsRequired();

        // 1-to-1 relationship with SupportRequest
        builder.HasOne(c => c.SupportRequest)
            .WithOne(r => r.Conversation)
            .HasForeignKey<Conversation>(c => c.SupportRequestId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(c => c.SupportRequestId)
            .IsUnique();
    }
}
