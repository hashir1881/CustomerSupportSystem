using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using CustomerSupport.Api.Services;
using CustomerSupport.Domain.Enums;
using Microsoft.AspNetCore.Http;

namespace CustomerSupport.Api.Tests;

public class ClaimsCurrentUserServiceTests
{
    [Fact]
    public void Properties_CorrectlyExtracted_FromAuthenticatedClaimsPrincipal()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var domainUsername = @"TIH\agent.sara";
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(JwtRegisteredClaimNames.UniqueName, domainUsername),
            new(ClaimTypes.Role, UserRole.CustomerService.ToString())
        };

        var identity = new ClaimsIdentity(claims, "Bearer");
        var principal = new ClaimsPrincipal(identity);

        var httpContext = new DefaultHttpContext { User = principal };
        var httpContextAccessor = new HttpContextAccessor { HttpContext = httpContext };

        var currentUserService = new ClaimsCurrentUserService(httpContextAccessor);

        // Act & Assert
        Assert.True(currentUserService.IsAuthenticated);
        Assert.Equal(userId, currentUserService.UserId);
        Assert.Equal(domainUsername, currentUserService.DomainUsername);
        Assert.Equal(UserRole.CustomerService, currentUserService.Role);
    }

    [Fact]
    public void HeaderFallback_ReturnsUsername_WhenClaimsAreEmpty()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["X-User-Username"] = @"CORP\test.header";
        var httpContextAccessor = new HttpContextAccessor { HttpContext = httpContext };

        var currentUserService = new ClaimsCurrentUserService(httpContextAccessor);

        // Act & Assert
        Assert.True(currentUserService.IsAuthenticated);
        Assert.Equal(@"CORP\test.header", currentUserService.DomainUsername);
        Assert.Null(currentUserService.UserId);
        Assert.Null(currentUserService.Role);
    }
}
