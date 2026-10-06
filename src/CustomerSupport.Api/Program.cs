using System.Text;
using CustomerSupport.Api.Middleware;
using CustomerSupport.Api.Services;
using CustomerSupport.Application;
using CustomerSupport.Application.Common.Interfaces;
using CustomerSupport.Infrastructure;
using CustomerSupport.Infrastructure.Common;
using CustomerSupport.Infrastructure.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.Negotiate;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// Register HTTP Context Accessor and Caller Identity
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, ClaimsCurrentUserService>();

// Register Authentication: Default scheme is JwtBearer, with Negotiate supported for Windows login
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
        ?? throw new InvalidOperationException("Jwt configuration section is missing.");

    options.RequireHttpsMetadata = false; // Dev / Intranet environment
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = jwtOptions.Issuer,
        ValidateAudience = true,
        ValidAudience = jwtOptions.Audience,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SecretKey)),
        ValidateLifetime = true,
        ClockSkew = TimeSpan.FromSeconds(30)
    };

    // SignalR Query String Token Extraction (Essential for Phase 8)
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;
            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
            {
                context.Token = accessToken;
            }
            return Task.CompletedTask;
        }
    };
})
.AddNegotiate(); // Windows Integrated Authentication (Kerberos / NTLM)

// Register Application & Infrastructure layers (Clean Architecture)
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddControllers();
builder.Services.AddSignalR();
builder.Services.AddOpenApi();

var app = builder.Build();

// Run Database Seeder on startup
using (var scope = app.Services.CreateScope())
{
    var seeder = scope.ServiceProvider.GetRequiredService<DatabaseSeeder>();
    await seeder.SeedAsync();
}

// Global Centralized Exception Handling Middleware
app.UseMiddleware<GlobalExceptionHandlerMiddleware>();

// Ensure uploads directory exists and configure static file serving
var webRoot = app.Environment.WebRootPath ?? Path.Combine(AppContext.BaseDirectory, "wwwroot");
var uploadsDir = Path.Combine(webRoot, "uploads");
Directory.CreateDirectory(uploadsDir);

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(uploadsDir),
    RequestPath = "/uploads"
});

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

// Authentication & Authorization pipeline
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<CustomerSupport.Api.Hubs.SupportChatHub>("/hubs/support-chat");

app.Run();
