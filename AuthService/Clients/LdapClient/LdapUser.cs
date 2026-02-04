namespace AuthService.Clients.LdapClient;

public sealed record LdapUser
{
    public string Username { get; }
    public string Domain { get; }
    public LdapAttributes Attributes { get; }

    public LdapUser(UserPassport passport, LdapAttributes attributes)
    {
        Username = passport.Username;
        Domain = passport.Domain;
        Attributes = attributes;
    }
}
