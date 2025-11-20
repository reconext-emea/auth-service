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
        // List<string> dc = Domain.Split('.', StringSplitOptions.RemoveEmptyEntries).ToList();
        // return $"CN={Username},DC={dc[0]},DC={dc[1]}";
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
