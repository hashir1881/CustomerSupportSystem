using CustomerSupport.Application.Common.Exceptions;
using CustomerSupport.Application.Common.Interfaces;
using CustomerSupport.Application.Dtos;
using CustomerSupport.Domain.Entities;
using CustomerSupport.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CustomerSupport.Application.Services;

public class SupportRequestService : ISupportRequestService
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public SupportRequestService(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<SupportRequestDto> CreateAsync(CreateSupportRequestDto dto, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Subject))
        {
            throw new ValidationException("Subject is required.");
        }

        if (dto.Subject.Length > 200)
        {
            throw new ValidationException("Subject cannot exceed 200 characters.");
        }

        if (string.IsNullOrWhiteSpace(dto.Description))
        {
            throw new ValidationException("Description is required.");
        }

        var currentUser = await GetRequiredCurrentUserAsync(cancellationToken);

        var request = new SupportRequest
        {
            Id = Guid.NewGuid(),
            RequesterUserId = currentUser.Id,
            Subject = dto.Subject.Trim(),
            Description = dto.Description.Trim(),
            Priority = dto.Priority,
            Status = RequestStatus.Pending,
            CreatedAt = DateTimeOffset.UtcNow
        };

        var conversation = new Conversation
        {
            Id = Guid.NewGuid(),
            SupportRequestId = request.Id,
            CreatedAt = DateTimeOffset.UtcNow
        };

        var auditLog = new AuditLog
        {
            Id = Guid.NewGuid(),
            UserId = currentUser.Id,
            Action = "ComplaintCreated",
            EntityName = nameof(SupportRequest),
            EntityId = request.Id.ToString(),
            Timestamp = DateTimeOffset.UtcNow,
            MetadataJson = System.Text.Json.JsonSerializer.Serialize(new { subject = request.Subject, priority = (int)request.Priority })
        };

        _context.SupportRequests.Add(request);
        _context.Conversations.Add(conversation);
        _context.AuditLogs.Add(auditLog);

        await _context.SaveChangesAsync(cancellationToken);

        return MapToDto(request, currentUser, null, conversation.Id);
    }

    public async Task<IReadOnlyList<SupportRequestDto>> GetAllAsync(RequestStatus? statusFilter = null, CancellationToken cancellationToken = default)
    {
        var currentUser = await GetRequiredCurrentUserAsync(cancellationToken);

        var query = _context.SupportRequests
            .AsNoTracking()
            .Include(r => r.RequesterUser)
            .Include(r => r.AssignedAgentUser)
            .Include(r => r.Conversation)
            .AsQueryable();

        // If caller is a regular Customer, enforce data isolation: customers only see their own tickets
        if (currentUser.Role == UserRole.Customer)
        {
            query = query.Where(r => r.RequesterUserId == currentUser.Id);
        }

        if (statusFilter.HasValue)
        {
            query = query.Where(r => r.Status == statusFilter.Value);
        }

        var items = await query
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(cancellationToken);

        return items.Select(r => MapToDto(r, r.RequesterUser, r.AssignedAgentUser, r.Conversation?.Id)).ToList();
    }

    public async Task<SupportRequestDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var currentUser = await GetRequiredCurrentUserAsync(cancellationToken);

        var request = await _context.SupportRequests
            .AsNoTracking()
            .Include(r => r.RequesterUser)
            .Include(r => r.AssignedAgentUser)
            .Include(r => r.Conversation)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

        if (request == null)
        {
            throw new NotFoundException(nameof(SupportRequest), id);
        }

        // Authorization check
        if (currentUser.Role == UserRole.Customer && request.RequesterUserId != currentUser.Id)
        {
            throw new ForbiddenException("You do not have permission to view this complaint.");
        }

        return MapToDto(request, request.RequesterUser, request.AssignedAgentUser, request.Conversation?.Id);
    }

    public async Task<SupportRequestDto> UpdateAsync(Guid id, UpdateSupportRequestDto dto, CancellationToken cancellationToken = default)
    {
        var currentUser = await GetRequiredCurrentUserAsync(cancellationToken);

        var request = await _context.SupportRequests
            .Include(r => r.RequesterUser)
            .Include(r => r.AssignedAgentUser)
            .Include(r => r.Conversation)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

        if (request == null)
        {
            throw new NotFoundException(nameof(SupportRequest), id);
        }

        if (currentUser.Role == UserRole.Customer && request.RequesterUserId != currentUser.Id)
        {
            throw new ForbiddenException("You cannot edit a complaint created by another user.");
        }

        if (request.Status != RequestStatus.Created && request.Status != RequestStatus.Pending)
        {
            throw new ConflictException($"Complaint #{id} cannot be edited because its current status is {request.Status}.");
        }

        if (!string.IsNullOrWhiteSpace(dto.Subject))
        {
            request.Subject = dto.Subject.Trim();
        }

        if (!string.IsNullOrWhiteSpace(dto.Description))
        {
            request.Description = dto.Description.Trim();
        }

        request.Priority = dto.Priority;
        request.UpdatedAt = DateTimeOffset.UtcNow;

        _context.AuditLogs.Add(new AuditLog
        {
            Id = Guid.NewGuid(),
            UserId = currentUser.Id,
            Action = "ComplaintUpdated",
            EntityName = nameof(SupportRequest),
            EntityId = request.Id.ToString(),
            Timestamp = DateTimeOffset.UtcNow
        });

        await _context.SaveChangesAsync(cancellationToken);

        return MapToDto(request, request.RequesterUser, request.AssignedAgentUser, request.Conversation?.Id);
    }

    public async Task<SupportRequestDto> ClaimAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var currentUser = await GetRequiredCurrentUserAsync(cancellationToken);

        // Security check: Only Customer Service or Admin can claim
        if (currentUser.Role != UserRole.CustomerService && currentUser.Role != UserRole.Administrator)
        {
            throw new ForbiddenException("Only Customer Service agents or Administrators can claim complaints.");
        }

        var request = await _context.SupportRequests
            .Include(r => r.RequesterUser)
            .Include(r => r.Conversation)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

        if (request == null)
        {
            throw new NotFoundException(nameof(SupportRequest), id);
        }

        // Business rule check: Can only claim Pending complaints
        if (request.Status != RequestStatus.Pending)
        {
            throw new ConflictException($"Complaint #{id} cannot be claimed because it is currently in '{request.Status}' status.");
        }

        if (request.AssignedAgentUserId.HasValue)
        {
            throw new ConflictException($"Complaint #{id} has already been claimed by another agent.");
        }

        // Apply state transition
        request.Status = RequestStatus.Claimed;
        request.AssignedAgentUserId = currentUser.Id;
        request.ClaimedAt = DateTimeOffset.UtcNow;
        request.UpdatedAt = DateTimeOffset.UtcNow;

        _context.AuditLogs.Add(new AuditLog
        {
            Id = Guid.NewGuid(),
            UserId = currentUser.Id,
            Action = "ComplaintClaimed",
            EntityName = nameof(SupportRequest),
            EntityId = request.Id.ToString(),
            Timestamp = DateTimeOffset.UtcNow,
            MetadataJson = System.Text.Json.JsonSerializer.Serialize(new { claimedBy = currentUser.DomainUsername, agentId = currentUser.Id.ToString() })
        });

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            // Optimistic concurrency collision: Another agent claimed this ticket at the exact same millisecond
            throw new ConflictException($"Conflict: Complaint #{id} was just claimed by another CS agent. Please refresh your dashboard.");
        }

        return MapToDto(request, request.RequesterUser, currentUser, request.Conversation?.Id);
    }

    public async Task<SupportRequestDto> ResolveAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var currentUser = await GetRequiredCurrentUserAsync(cancellationToken);

        if (currentUser.Role != UserRole.CustomerService && currentUser.Role != UserRole.Administrator)
        {
            throw new ForbiddenException("Only Customer Service agents can mark complaints as resolved.");
        }

        var request = await _context.SupportRequests
            .Include(r => r.RequesterUser)
            .Include(r => r.AssignedAgentUser)
            .Include(r => r.Conversation)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

        if (request == null)
        {
            throw new NotFoundException(nameof(SupportRequest), id);
        }

        if (request.Status != RequestStatus.Claimed && request.Status != RequestStatus.InProgress && request.Status != RequestStatus.WaitingForCustomer)
        {
            throw new ConflictException($"Complaint #{id} cannot be resolved from its current status '{request.Status}'.");
        }

        request.Status = RequestStatus.Resolved;
        request.ResolvedAt = DateTimeOffset.UtcNow;
        request.UpdatedAt = DateTimeOffset.UtcNow;

        _context.AuditLogs.Add(new AuditLog
        {
            Id = Guid.NewGuid(),
            UserId = currentUser.Id,
            Action = "ComplaintResolved",
            EntityName = nameof(SupportRequest),
            EntityId = request.Id.ToString(),
            Timestamp = DateTimeOffset.UtcNow
        });

        await _context.SaveChangesAsync(cancellationToken);

        return MapToDto(request, request.RequesterUser, request.AssignedAgentUser, request.Conversation?.Id);
    }

    public async Task<SupportRequestDto> CloseAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var currentUser = await GetRequiredCurrentUserAsync(cancellationToken);

        var request = await _context.SupportRequests
            .Include(r => r.RequesterUser)
            .Include(r => r.AssignedAgentUser)
            .Include(r => r.Conversation)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

        if (request == null)
        {
            throw new NotFoundException(nameof(SupportRequest), id);
        }

        // Customer can only close their own ticket if resolved or pending
        if (currentUser.Role == UserRole.Customer && request.RequesterUserId != currentUser.Id)
        {
            throw new ForbiddenException("You cannot close a complaint that does not belong to you.");
        }

        if (request.Status == RequestStatus.Closed)
        {
            throw new ConflictException($"Complaint #{id} is already closed.");
        }

        request.Status = RequestStatus.Closed;
        request.ClosedAt = DateTimeOffset.UtcNow;
        request.UpdatedAt = DateTimeOffset.UtcNow;

        _context.AuditLogs.Add(new AuditLog
        {
            Id = Guid.NewGuid(),
            UserId = currentUser.Id,
            Action = "ComplaintClosed",
            EntityName = nameof(SupportRequest),
            EntityId = request.Id.ToString(),
            Timestamp = DateTimeOffset.UtcNow
        });

        await _context.SaveChangesAsync(cancellationToken);

        return MapToDto(request, request.RequesterUser, request.AssignedAgentUser, request.Conversation?.Id);
    }

    private async Task<User> GetRequiredCurrentUserAsync(CancellationToken cancellationToken)
    {
        if (!_currentUserService.IsAuthenticated)
        {
            throw new ForbiddenException("Unauthenticated request. A valid Windows domain identity is required.");
        }

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.DomainUsername == _currentUserService.DomainUsername, cancellationToken);

        if (user == null)
        {
            throw new NotFoundException($"User with domain username '{_currentUserService.DomainUsername}' was not found in the application database.");
        }

        return user;
    }

    private static SupportRequestDto MapToDto(SupportRequest request, User requester, User? assignedAgent, Guid? conversationId)
    {
        return new SupportRequestDto
        {
            Id = request.Id,
            Subject = request.Subject,
            Description = request.Description,
            Status = request.Status,
            Priority = request.Priority,
            CreatedAt = request.CreatedAt,
            UpdatedAt = request.UpdatedAt,
            ClaimedAt = request.ClaimedAt,
            ResolvedAt = request.ResolvedAt,
            ClosedAt = request.ClosedAt,
            ConversationId = conversationId,
            Requester = new UserDto
            {
                Id = requester.Id,
                DomainUsername = requester.DomainUsername,
                DisplayName = requester.DisplayName,
                Email = requester.Email,
                Department = requester.Department,
                Role = requester.Role
            },
            AssignedAgent = assignedAgent == null ? null : new UserDto
            {
                Id = assignedAgent.Id,
                DomainUsername = assignedAgent.DomainUsername,
                DisplayName = assignedAgent.DisplayName,
                Email = assignedAgent.Email,
                Department = assignedAgent.Department,
                Role = assignedAgent.Role
            }
        };
    }

    private static string EscapeJson(string? input) =>
        input?.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", " ").Replace("\r", "") ?? "";
}
