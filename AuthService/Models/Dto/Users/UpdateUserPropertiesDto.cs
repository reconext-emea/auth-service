using AuthService.Constants;

namespace AuthService.Models.Dto.Users;

public sealed record UpdateUserPropertiesDto
{
    public string Confidentiality { get; init; } = ConfidentialityClass.Class1;
    public string[] Programs { get; init; } = [];
}
