using CustomerSupport.Application.Common.Interfaces;
using CustomerSupport.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CustomerSupport.Infrastructure.Data;

/// <summary>
/// The EF Core DbContext for the Customer Support System.
/// Implements IApplicationDbContext to expose database operations to the Application layer.
/// </summary>
public class CustomerSupportDbContext : DbContext, IApplicationDbContext
{
    public CustomerSupportDbContext(DbContextOptions<CustomerSupportDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<SupportRequest> SupportRequests => Set<SupportRequest>();
    public DbSet<Conversation> Conversations => Set<Conversation>();
    public DbSet<Message> Messages => Set<Message>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Automatically discover and apply all IEntityTypeConfiguration classes in this assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CustomerSupportDbContext).Assembly);
    }
}
