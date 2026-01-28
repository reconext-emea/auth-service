namespace AuthService.Models.Dto.Users;

public sealed record AuthServiceUserCustomPropertiesDto(
    string Confidentiality,
    string Region,
    IReadOnlyList<string> Programs
)
{
    public static AuthServiceUserCustomPropertiesDto From(AuthServiceUserCustomProperties p) =>
        new(p.Confidentiality, p.Region, [.. p.Programs ?? []]);
}
