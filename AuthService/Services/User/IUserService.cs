using AuthService.Models;
using AuthService.Models.Dto.Users;

namespace AuthService.Services.User;

public interface IUserService
{
    Task<AuthServiceUser?> FindUserByIdentifierAsync(string userIdentifier);
    Task<AuthServiceUser?> FindUserByIdentifierAsync(
        IQueryable<AuthServiceUser> query,
        string userIdentifier
    );
    Task ImportSingleUserAsync(
        ImportUserDto import,
        ImportUsersResponseDto response,
        CancellationToken cancellationToken = default
    );
}
