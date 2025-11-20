namespace AuthService.Models.Requests.Ldap;

public record LdapLoginRequest(string Username, string Domain, string Password);
