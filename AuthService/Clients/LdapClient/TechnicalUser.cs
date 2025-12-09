namespace AuthService.Clients.LdapClient;

public class TechnicalUser(string username, string domain, string password)
{
    private string Username { get; } = username;
    private string Domain { get; } = domain;
    private string Password { get; } = password;

    public string GetHost()
    {
        return Domain;
    }

    public string GetBindDn()
    {
        return $"{Username}@{Domain}";
    }

    public string GetBindPassword()
    {
        return Password;
    }

    public string GetSearchFilter(string username)
    {
        return $"(sAMAccountName={username})";
    }
}
