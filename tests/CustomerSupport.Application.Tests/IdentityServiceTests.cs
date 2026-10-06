using CustomerSupport.Application.Common.Exceptions;
using CustomerSupport.Application.Services;
using CustomerSupport.Domain.Entities;
using CustomerSupport.Domain.Enums;
using CustomerSupport.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CustomerSupport.Application.Tests;

public class IdentityServiceTests
{
    private static CustomerSupportDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<CustomerSupportDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new CustomerSupportDbContext(options);
    }

    [Fact]
    public async Task AuthenticateDomainUserAsync_WhenUserExists_ShouldReturnExistingUserWithServerRole()
    {
        // Arrange
        using var context = CreateDbContext();
        var agent = new User
        {
            Id = Guid.NewGuid(),
            DomainUsername = @"CORP\agent.sara",
            DisplayName = "Sara Ali",
            Email = "sara@company.corp",
            Department = "CS Tier 1",
            Role = UserRole.CustomerService,
            IsActive = true
        };
        context.Users.Add(agent);
        await context.SaveChangesAsync();

        var service = new IdentityService(context, NullLogger<IdentityService>.Instance);

        // Act
        var result = await service.AuthenticateDomainUserAsync(@"CORP\agent.sara");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(agent.Id, result.Id);
        Assert.Equal(UserRole.CustomerService, result.Role);
        Assert.Equal("Sara Ali", result.DisplayName);

        var updatedUser = await context.Users.FindAsync(agent.Id);
        Assert.NotNull(updatedUser?.LastLoginAt);
    }

    [Fact]
    public async Task AuthenticateDomainUserAsync_WhenUserDoesNotExist_ShouldAutoProvisionAsCustomer()
    {
        // Arrange
        using var context = CreateDbContext();
        var service = new IdentityService(context, NullLogger<IdentityService>.Instance);

        // Act
        var result = await service.AuthenticateDomainUserAsync(@"CORP\tariq.mehmood");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(@"CORP\tariq.mehmood", result.DomainUsername);
        Assert.Equal(UserRole.Customer, result.Role); // Server assigns Customer role!
        Assert.Equal("Tariq Mehmood", result.DisplayName);

        var userInDb = await context.Users.FirstOrDefaultAsync(u => u.DomainUsername == @"CORP\tariq.mehmood");
        Assert.NotNull(userInDb);
        Assert.Equal(UserRole.Customer, userInDb.Role);
    }

    [Fact]
    public async Task AuthenticateDomainUserAsync_WhenUserIsDeactivated_ShouldThrowForbiddenException()
    {
        // Arrange
        using var context = CreateDbContext();
        var disabledUser = new User
        {
            Id = Guid.NewGuid(),
            DomainUsername = @"CORP\former.employee",
            DisplayName = "Former Employee",
            Role = UserRole.Customer,
            IsActive = false // Deactivated!
        };
        context.Users.Add(disabledUser);
        await context.SaveChangesAsync();

        var service = new IdentityService(context, NullLogger<IdentityService>.Instance);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ForbiddenException>(() =>
            service.AuthenticateDomainUserAsync(@"CORP\former.employee"));

        Assert.Contains("deactivated", ex.Message);
    }

    [Fact]
    public async Task AuthenticateDomainUserAsync_WhenUsernameIsEmpty_ShouldThrowValidationException()
    {
        // Arrange
        using var context = CreateDbContext();
        var service = new IdentityService(context, NullLogger<IdentityService>.Instance);

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() =>
            service.AuthenticateDomainUserAsync("   "));
    }
}
