using AuthService.Models.Dto.Errors;
using Microsoft.AspNetCore.Identity;

namespace AuthService.Models.Dto.Users;

public sealed class ImportUsersResponseDto
{
    public int Created { get; private set; }
    public int Skipped { get; private set; }

    public IReadOnlyList<ErrorResponseDto> Errors => _errors;
    private readonly List<ErrorResponseDto> _errors = [];

    public string Message => $"Created: {Created}, Skipped: {Skipped}, Errors: {_errors.Count}";

    public void AddCreated(int count = 1)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        Created += count;
    }

    public void AddSkipped(int count = 1)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        Skipped += count;
    }

    public void AddError(string username, ErrorResponseDto error)
    {
        _errors.Add(
            new ErrorResponseDto
            {
                Error = $"Failed importing user '{username}': {error.Error}",
                Details = error.Details,
            }
        );
    }

    public void AddError(string username, IEnumerable<IdentityError> identityErrors)
    {
        var details = identityErrors is null
            ? null
            : string.Join(", ", identityErrors.Select(e => e.Description));

        _errors.Add(
            new ErrorResponseDto
            {
                Error = $"Failed importing user '{username}'.",
                Details = details,
            }
        );
    }

    public void AddException(string username, Exception exception)
    {
        _errors.Add(
            new ErrorResponseDto
            {
                Error = $"Unexpected error while importing user '{username}'.",
                Details = exception.Message,
            }
        );
    }
}
