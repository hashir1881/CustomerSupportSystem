using CustomerSupport.Application.Common.Exceptions;
using CustomerSupport.Application.Common.Interfaces;
using CustomerSupport.Application.Dtos;
using CustomerSupport.Application.Services;
using CustomerSupport.Domain.Entities;
using CustomerSupport.Domain.Enums;
using CustomerSupport.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CustomerSupport.Application.Tests;

public class SupportRequestServiceTests
{
    private class TestCurrentUserService : ICurrentUserService
    {
        public Guid? UserId { get; set; }
        public string? DomainUsername { get; set; }
        public UserRole? Role { get; set; }
        public bool IsAuthenticated => !string.IsNullOrEmpty(DomainUsername);
    }

    private static CustomerSupportDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<CustomerSupportDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new CustomerSupportDbContext(options);
    }

    [Fact]
    public async Task CreateAsync_WithValidDto_ShouldCreatePendingRequestAndConversation()
    {
        // Arrange
        using var context = CreateDbContext();
        var user = new User
        {
            Id = Guid.NewGuid(),
            DomainUsername = @"CORP\customer.test",
            DisplayName = "Test Customer",
            Email = "customer@test.corp",
            Department = "IT",
            Role = UserRole.Customer
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var currentUserService = new TestCurrentUserService
        {
            UserId = user.Id,
            DomainUsername = user.DomainUsername,
            Role = user.Role
        };

        var service = new SupportRequestService(context, currentUserService);
        var dto = new CreateSupportRequestDto
        {
            Subject = "Printer not working",
            Description = "Paper jam on 3rd floor printer",
            Priority = RequestPriority.High
        };

        // Act
        var result = await service.CreateAsync(dto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Printer not working", result.Subject);
        Assert.Equal(RequestStatus.Pending, result.Status);
        Assert.Equal(user.Id, result.Requester.Id);
        Assert.NotNull(result.ConversationId);

        var savedInDb = await context.SupportRequests.FindAsync(result.Id);
        Assert.NotNull(savedInDb);
        Assert.Equal(RequestStatus.Pending, savedInDb.Status);
    }

    [Fact]
    public async Task CreateAsync_WithEmptySubject_ShouldThrowValidationException()
    {
        // Arrange
        using var context = CreateDbContext();
        var currentUserService = new TestCurrentUserService
        {
            DomainUsername = @"CORP\customer.test"
        };
        var service = new SupportRequestService(context, currentUserService);

        var dto = new CreateSupportRequestDto
        {
            Subject = "",
            Description = "Description with no subject"
        };

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() => service.CreateAsync(dto));
    }

    [Fact]
    public async Task ClaimAsync_WhenPending_ShouldAssignAgentAndSetClaimedStatus()
    {
        // Arrange
        using var context = CreateDbContext();
        var customer = new User
        {
            Id = Guid.NewGuid(),
            DomainUsername = @"CORP\customer.test",
            DisplayName = "Customer",
            Email = "cust@test.corp",
            Role = UserRole.Customer
        };
        var agent = new User
        {
            Id = Guid.NewGuid(),
            DomainUsername = @"CORP\agent.sara",
            DisplayName = "Sara Agent",
            Email = "sara@test.corp",
            Role = UserRole.CustomerService
        };
        var request = new SupportRequest
        {
            Id = Guid.NewGuid(),
            RequesterUserId = customer.Id,
            Subject = "Network down",
            Description = "WiFi connection failing",
            Status = RequestStatus.Pending,
            Priority = RequestPriority.Urgent
        };

        context.Users.AddRange(customer, agent);
        context.SupportRequests.Add(request);
        await context.SaveChangesAsync();

        var currentUserService = new TestCurrentUserService
        {
            UserId = agent.Id,
            DomainUsername = agent.DomainUsername,
            Role = agent.Role
        };

        var service = new SupportRequestService(context, currentUserService);

        // Act
        var result = await service.ClaimAsync(request.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(RequestStatus.Claimed, result.Status);
        Assert.NotNull(result.AssignedAgent);
        Assert.Equal(agent.Id, result.AssignedAgent.Id);
        Assert.NotNull(result.ClaimedAt);
    }

    [Fact]
    public async Task ClaimAsync_WhenAlreadyClaimed_ShouldThrowConflictException()
    {
        // Arrange
        using var context = CreateDbContext();
        var customer = new User
        {
            Id = Guid.NewGuid(),
            DomainUsername = @"CORP\customer.test",
            Role = UserRole.Customer
        };
        var agent1 = new User
        {
            Id = Guid.NewGuid(),
            DomainUsername = @"CORP\agent.sara",
            Role = UserRole.CustomerService
        };
        var agent2 = new User
        {
            Id = Guid.NewGuid(),
            DomainUsername = @"CORP\agent.bilal",
            Role = UserRole.CustomerService
        };
        var request = new SupportRequest
        {
            Id = Guid.NewGuid(),
            RequesterUserId = customer.Id,
            AssignedAgentUserId = agent1.Id,
            Subject = "Screen issue",
            Description = "Monitor flickering",
            Status = RequestStatus.Claimed // Already claimed!
        };

        context.Users.AddRange(customer, agent1, agent2);
        context.SupportRequests.Add(request);
        await context.SaveChangesAsync();

        var currentUserService = new TestCurrentUserService
        {
            UserId = agent2.Id,
            DomainUsername = agent2.DomainUsername,
            Role = agent2.Role
        };

        var service = new SupportRequestService(context, currentUserService);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ConflictException>(() => service.ClaimAsync(request.Id));
        Assert.Contains("cannot be claimed", ex.Message);
    }

    [Fact]
    public async Task ClaimAsync_WhenUserIsCustomer_ShouldThrowForbiddenException()
    {
        // Arrange
        using var context = CreateDbContext();
        var customer = new User
        {
            Id = Guid.NewGuid(),
            DomainUsername = @"CORP\customer.test",
            Role = UserRole.Customer
        };
        var request = new SupportRequest
        {
            Id = Guid.NewGuid(),
            RequesterUserId = customer.Id,
            Subject = "Login issue",
            Description = "Cannot access portal",
            Status = RequestStatus.Pending
        };

        context.Users.Add(customer);
        context.SupportRequests.Add(request);
        await context.SaveChangesAsync();

        var currentUserService = new TestCurrentUserService
        {
            UserId = customer.Id,
            DomainUsername = customer.DomainUsername,
            Role = customer.Role // Customer trying to claim!
        };

        var service = new SupportRequestService(context, currentUserService);

        // Act & Assert
        await Assert.ThrowsAsync<ForbiddenException>(() => service.ClaimAsync(request.Id));
    }
}
