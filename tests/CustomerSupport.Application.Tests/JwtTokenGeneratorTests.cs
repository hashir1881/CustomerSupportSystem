using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using CustomerSupport.Application.Dtos;
using CustomerSupport.Domain.Enums;
using CustomerSupport.Infrastructure.Common;
using CustomerSupport.Infrastructure.Services;
using Microsoft.Extensions.Options;

namespace CustomerSupport.Application.Tests;

public class JwtTokenGeneratorTests
{
    private readonly JwtOptions _validOptions = new()
    {
        Issuer = "CustomerSupportApi",
        Audience = "CustomerSupportClients",
        SecretKey = "SuperSecretKey_For_CustomerSupport_2026_Secure_Key_MustBeLongerThan32Chars!",
        ExpiryMinutes = 60
    };

    [Fact]
    public void GenerateToken_ReturnsValidSignedJwt_WithAllRequiredClaims()
    {
        // Arrange
        var optionsMock = Options.Create(_validOptions);
        var generator = new JwtTokenGenerator(optionsMock);

        var user = new UserDto
        {
            Id = Guid.NewGuid(),
            DomainUsername = @"TIH\test.user",
            DisplayName = "Test User",
            Email = "test.user@tih.org.pk",
            Department = "IT Support",
            Role = UserRole.CustomerService
        };

        // Act
        var (token, expiresAt) = generator.GenerateToken(user);

        // Assert
        Assert.NotNull(token);
        Assert.True(expiresAt > DateTimeOffset.UtcNow);

        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);

        Assert.Equal(_validOptions.Issuer, jwt.Issuer);
        Assert.Contains(_validOptions.Audience, jwt.Audiences);

        Assert.Equal(user.Id.ToString(), jwt.Claims.First(c => c.Type == JwtRegisteredClaimNames.Sub).Value);
        Assert.Equal(user.DomainUsername, jwt.Claims.First(c => c.Type == JwtRegisteredClaimNames.UniqueName).Value);
        Assert.Equal(UserRole.CustomerService.ToString(), jwt.Claims.First(c => c.Type == ClaimTypes.Role || c.Type == "role").Value);
        Assert.Equal(user.Department, jwt.Claims.First(c => c.Type == "department").Value);
    }

    [Fact]
    public void Constructor_ThrowsException_WhenKeyIsLessThan32Characters()
    {
        // Arrange
        var invalidOptions = Options.Create(new JwtOptions
        {
            Issuer = "Api",
            Audience = "Clients",
            SecretKey = "TooShortKey" // < 32 chars
        });

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => new JwtTokenGenerator(invalidOptions));
    }
}
