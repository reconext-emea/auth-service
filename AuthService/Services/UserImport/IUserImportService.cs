using AuthService.Models.Dto.Users;

namespace AuthService.Services.UserImport;

public interface IUserImportService
{
    Task ImportSingleUserAsync(
        ImportUserDto import,
        ImportUsersResponseDto response,
        CancellationToken cancellationToken = default
    );
}
