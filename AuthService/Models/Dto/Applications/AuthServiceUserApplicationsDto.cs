// namespace AuthService.Models.Dto.Applications;

// public sealed record AuthServiceUserApplicationsDto(IReadOnlyList<ApplicationDto> Applications)
// {
//     public static AuthServiceUserApplicationsDto From(
//         ICollection<AuthServiceUserApplication> apps
//     ) =>
//         new([
//             .. apps.Select(app => new ApplicationDto(
//                 app.Application.Id!,
//                 app.Application.ClientId,
//                 app.Application.DisplayName
//             )),
//         ]);
// }
