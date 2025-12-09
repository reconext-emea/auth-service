namespace AuthService.Clients.EntraIdClient;

public class EntraIdConfig(IConfiguration config)
{
    public string TenantId { get; } = RequireEnv(config, "AzureAd", "TenantId");
    public string ClientId { get; } = RequireEnv(config, "AzureAd", "ClientId");

    public string IssuerV2 => $"https://login.microsoftonline.com/{TenantId}/v2.0";

    private static string RequireEnv(IConfiguration config, string section, string key)
    {
        string? value = config.GetSection(section)[key];

        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException($"Missing required environment variable: {key}");

        return value;
    }
}
