using CustomerSupport.Application.Dtos;
using CustomerSupport.Domain.Enums;

namespace CustomerSupport.Application.Tests;

public class SupportRequestDtoTests
{
    [Fact]
    public void Properties_FormatCorrectly_AndCanBeClosedEvaluatesAccurately()
    {
        // Arrange
        var id = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var dto = new SupportRequestDto
        {
            Id = id,
            Subject = "Test Issue",
            Description = "Test details",
            Priority = RequestPriority.High,
            Status = RequestStatus.InProgress,
            CreatedAt = now,
            ClaimedAt = now.AddMinutes(5),
            ResolvedAt = null,
            ClosedAt = null
        };

        // Act & Assert
        Assert.Equal(id.ToString(), dto.IdString);
        Assert.Equal("High", dto.PriorityName);
        Assert.Equal("InProgress", dto.StatusName);
        Assert.True(dto.CanBeClosed);
        Assert.False(string.IsNullOrWhiteSpace(dto.CreatedAtFormatted));
        Assert.False(string.IsNullOrWhiteSpace(dto.ClaimedAtFormatted));
        Assert.Null(dto.ResolvedAtFormatted);
        Assert.Null(dto.ClosedAtFormatted);

        // When closed, CanBeClosed becomes false
        dto.Status = RequestStatus.Closed;
        dto.ClosedAt = now.AddMinutes(30);
        Assert.False(dto.CanBeClosed);
        Assert.NotNull(dto.ClosedAtFormatted);
    }
}
