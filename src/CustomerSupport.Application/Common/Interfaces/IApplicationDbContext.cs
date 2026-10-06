using CustomerSupport.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CustomerSupport.Application.Common.Interfaces;

/// <summary>
/// Abstraction for database operations.
/// Allows Application use-cases to query and persist entities without depending on concrete EF Core DbContext or PostgreSQL.
/// </summary>
public interface IApplicationDbContext
{
    DbSet<User> Users { get; }
    DbSet<SupportRequest> SupportRequests { get; }
    DbSet<Conversation> Conversations { get; }
    DbSet<Message> Messages { get; }
    DbSet<AuditLog> AuditLogs { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
