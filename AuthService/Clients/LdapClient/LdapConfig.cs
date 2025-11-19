namespace AuthService.Clients.LdapClient;

public class LdapConfig(IConfiguration config)
{
    public Dictionary<string, string> AllowedDomains { get; } = GetAllowedDomains(config);
    public List<string> AllowedEmeaOfficeNames { get; } = GetAllowedEmeaOfficeNames(config);

    public TechnicalUser TechnicalUser { get; } =
        new(
            RequireEnv(config, "Ldap__TechnicalUsername"),
            RequireEnv(config, "Ldap__TechnicalDomain"),
            RequireEnv(config, "Ldap__TechnicalPassword")
        );

    private static Dictionary<string, string> GetAllowedDomains(IConfiguration config)
    {
        string value = RequireEnv(config, "Ldap__AllowedDomains");
        IEnumerable<string> enumerableValue = ToEnumerable(value);
        return enumerableValue.ToDictionary(
            domain => domain,
            domain => string.Join(",", domain.Split('.').Select(p => $"dc={p}"))
        );
    }

    private static List<string> GetAllowedEmeaOfficeNames(IConfiguration config)
    {
        string value = RequireEnv(config, "Ldap__AllowedEmeaOfficeNames");
        IEnumerable<string> enumerableValue = ToEnumerable(value);
        return enumerableValue.ToList();
    }

    private static string RequireEnv(IConfiguration config, string key)
    {
        string? value = config[key];

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
