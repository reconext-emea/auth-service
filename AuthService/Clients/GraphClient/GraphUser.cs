namespace AuthService.Clients.GraphClient;

// public record GraphUser
// {
//     public string Username { get; }
//     public string Mail { get; }
//     public string OfficeLocation { get; }

//     public GraphUser(string username, string mail, string officeLocation)
//     {
//         Username = username;
//         Mail = mail;
//         OfficeLocation = officeLocation;
//     }
// }

public sealed record GraphUser
{
    public string Username { get; }
    public string Mail { get; }
    public GraphAttributes Attributes { get; }

    public string? OfficeLocation => Attributes.OfficeLocation;

    public GraphUser(string username, string mail, GraphAttributes attributes)
    {
        Username = username;
        Mail = mail;
        Attributes = attributes;
    }
}
