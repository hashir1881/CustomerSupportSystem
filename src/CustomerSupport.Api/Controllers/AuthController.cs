using CustomerSupport.Application.Common.Exceptions;
using CustomerSupport.Application.Common.Interfaces;
using CustomerSupport.Application.Dtos;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Negotiate;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CustomerSupport.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IIdentityService _identityService;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly IWebHostEnvironment _environment;

    public AuthController(
        IIdentityService identityService,
        IJwtTokenGenerator jwtTokenGenerator,
        IWebHostEnvironment environment)
    {
        _identityService = identityService;
        _jwtTokenGenerator = jwtTokenGenerator;
        _environment = environment;
    }

    /// <summary>
    /// Authenticates the user via Active Directory / Windows Integrated Authentication (Negotiate).
    /// Extracts the verified Windows identity, maps to PostgreSQL, determines the server-enforced role,
    /// signs and returns the application JWT.
    /// </summary>
    [HttpPost("windows-login")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponseDto>> WindowsLogin(CancellationToken cancellationToken)
    {
        // 1. Explicitly authenticate via Negotiate scheme if credentials were sent
        var negotiateResult = await HttpContext.AuthenticateAsync(NegotiateDefaults.AuthenticationScheme);
        string? domainUsername = negotiateResult.Succeeded && negotiateResult.Principal?.Identity?.IsAuthenticated == true
            ? negotiateResult.Principal.Identity.Name
            : (User.Identity?.IsAuthenticated == true ? User.Identity.Name : null);

        // 2. In development mode, allow header simulation or fallback to local Windows machine logon
        if (string.IsNullOrWhiteSpace(domainUsername) && _environment.IsDevelopment())
        {
            var headerOverride = Request.Headers["X-User-Username"].FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(headerOverride))
            {
                domainUsername = headerOverride.Trim();
            }
            else
            {
                // Fallback to local Windows account of the current machine
                domainUsername = $@"{Environment.UserDomainName}\{Environment.UserName}";
            }
        }

        if (string.IsNullOrWhiteSpace(domainUsername))
        {
            // Prompt the client for Windows credentials via Negotiate challenge
            return Challenge(NegotiateDefaults.AuthenticationScheme);
        }

        var user = await _identityService.AuthenticateDomainUserAsync(domainUsername, cancellationToken);
        var (token, expiresAt) = _jwtTokenGenerator.GenerateToken(user);

        return Ok(new AuthResponseDto
        {
            Token = token,
            TokenType = "Bearer",
            ExpiresAt = expiresAt,
            User = user
        });
    }

    /// <summary>
    /// Authenticates using corporate Active Directory credentials (Domain\Username + Password).
    /// Used when a user signs out and logs in via the interactive desktop login screen.
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponseDto>> Login(
        [FromBody] LoginRequestDto dto,
        CancellationToken cancellationToken)
    {
        var user = await _identityService.AuthenticateCredentialsAsync(dto.Username, dto.Password, cancellationToken);
        var (token, expiresAt) = _jwtTokenGenerator.GenerateToken(user);

        return Ok(new AuthResponseDto
        {
            Token = token,
            TokenType = "Bearer",
            ExpiresAt = expiresAt,
            User = user
        });
    }

    /// <summary>
    /// Diagnostics endpoint returning the raw identity and claims detected by the web server.
    /// </summary>
    [HttpGet("whoami")]
    [AllowAnonymous]
    public async Task<ActionResult<object>> WhoAmI()
    {
        var negotiateResult = await HttpContext.AuthenticateAsync(NegotiateDefaults.AuthenticationScheme);

        return Ok(new
        {
            IsAuthenticated = User.Identity?.IsAuthenticated ?? false,
            AuthenticationType = User.Identity?.AuthenticationType,
            Name = User.Identity?.Name,
            NegotiateSucceeded = negotiateResult.Succeeded,
            NegotiateName = negotiateResult.Principal?.Identity?.Name,
            MachineDomain = Environment.UserDomainName,
            MachineUser = Environment.UserName,
            LocalWindowsIdentity = $@"{Environment.UserDomainName}\{Environment.UserName}",
            Claims = User.Claims.Select(c => new { c.Type, c.Value })
        });
    }
}
