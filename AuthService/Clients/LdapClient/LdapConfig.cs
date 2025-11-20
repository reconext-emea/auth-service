namespace AuthService.Clients.LdapClient;

public class LdapConfig(IConfiguration config)
{
    public Dictionary<string, string> AllowedDomains { get; } = GetAllowedDomains(config);
    public List<string> AllowedEmeaOfficeNames { get; } = GetAllowedEmeaOfficeNames(config);

    public TechnicalUser TechnicalUser { get; } =
        new(
            RequireEnv(config, "Ldap", "TechnicalUsername"),
            RequireEnv(config, "Ldap", "TechnicalDomain"),
            RequireEnv(config, "Ldap", "TechnicalPassword")
        );

    private static Dictionary<string, string> GetAllowedDomains(IConfiguration config)
    {
        string value = RequireEnv(config, "Ldap", "AllowedDomains");
        IEnumerable<string> enumerableValue = ToEnumerable(value);
        return enumerableValue.ToDictionary(
            domain => domain,
            domain => string.Join(",", domain.Split('.').Select(p => $"dc={p}"))
        );
    }

    private static List<string> GetAllowedEmeaOfficeNames(IConfiguration config)
    {
        string value = RequireEnv(config, "Ldap", "AllowedEmeaOfficeNames");
        IEnumerable<string> enumerableValue = ToEnumerable(value);
        return enumerableValue.ToList();
    }

    private static string RequireEnv(IConfiguration config, string section, string key)
    {
        string? value = config.GetSection(section)[key];

        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException($"Missing required environment variable: {key}");

        return value;
    }

    private static IEnumerable<string> ToEnumerable(string value)
    {
        return value
            .Split("::", StringSplitOptions.RemoveEmptyEntries)
            .Select(domain => domain.Trim())
            .Where(domain => domain.Length > 0);
    }
}
