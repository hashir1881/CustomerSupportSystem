using CustomerSupport.Domain.Entities;
using CustomerSupport.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CustomerSupport.Infrastructure.Data;

public class DatabaseSeeder
{
    private readonly CustomerSupportDbContext _context;
    private readonly ILogger<DatabaseSeeder> _logger;

    public DatabaseSeeder(CustomerSupportDbContext context, ILogger<DatabaseSeeder> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        if (await _context.Users.AnyAsync(cancellationToken))
        {
            return; // Already seeded
        }

        _logger.LogInformation("Seeding initial enterprise users into CustomerSupportDb...");

        var users = new List<User>
        {
            new User
            {
                Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                DomainUsername = @"CORP\customer.ahmed",
                DisplayName = "Ahmed Khan",
                Email = "ahmed.khan@company.corp",
                Department = "Sales",
                Role = UserRole.Customer,
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow
            },
            new User
            {
                Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                DomainUsername = @"CORP\agent.sara",
                DisplayName = "Sara Ali",
                Email = "sara.ali@company.corp",
                Department = "Customer Support Tier 1",
                Role = UserRole.CustomerService,
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow
            },
            new User
            {
                Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                DomainUsername = @"CORP\agent.bilal",
                DisplayName = "Bilal Hassan",
                Email = "bilal.hassan@company.corp",
                Department = "Customer Support Tier 2",
                Role = UserRole.CustomerService,
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow
            },
            new User
            {
                Id = Guid.Parse("44444444-4444-4444-4444-444444444444"),
                DomainUsername = @"CORP\admin.sys",
                DisplayName = "System Administrator",
                Email = "admin@company.corp",
                Department = "IT Infrastructure",
                Role = UserRole.Administrator,
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow
            },
            new User
            {
                Id = Guid.Parse("55555555-5555-5555-5555-555555555555"),
                DomainUsername = @"TIH\agent.sara",
                DisplayName = "Sara Ali",
                Email = "sara.ali@company.tih",
                Department = "Customer Support Tier 1",
                Role = UserRole.CustomerService,
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow
            },
            new User
            {
                Id = Guid.Parse("66666666-6666-6666-6666-666666666666"),
                DomainUsername = @"TIH\agent.ali",
                DisplayName = "Ali Raza",
                Email = "ali.raza@company.tih",
                Department = "Customer Support Tier 2",
                Role = UserRole.CustomerService,
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow
            },
            new User
            {
                Id = Guid.Parse("77777777-7777-7777-7777-777777777777"),
                DomainUsername = @"TIH\admin.user",
                DisplayName = "Admin User",
                Email = "admin@company.tih",
                Department = "IT Infrastructure",
                Role = UserRole.Administrator,
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow
            }
        };

        _context.Users.AddRange(users);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Seeding complete. Added {Count} default enterprise users.", users.Count);
    }
}
