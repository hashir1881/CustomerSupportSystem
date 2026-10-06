namespace CustomerSupport.Infrastructure.Common;

public class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "CustomerSupportApi";
    public string Audience { get; set; } = "CustomerSupportClients";
    public string SecretKey { get; set; } = string.Empty;
    public int ExpiryMinutes { get; set; } = 30;
}
