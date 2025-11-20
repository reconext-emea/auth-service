// using AuthService.Clients.LdapClient;
// using AuthService.Models;
// using AuthService.Models.Requests.Ldap;
// using AuthService.Services.Token;
// using Microsoft.AspNetCore.Identity;
// using Microsoft.AspNetCore.Mvc;
// using Microsoft.EntityFrameworkCore;

// namespace AuthService.Controllers;

// [ApiController]
// [Route("api/auth/ldap")]
// public class LdapAuthController(
//     LdapClient ldap,
//     UserManager<AuthServiceUser> userManager,
//     ITokenService tokenService
// ) : ControllerBase
// {
//     private readonly LdapClient _ldap = ldap;
//     private readonly UserManager<AuthServiceUser> _userManager = userManager;
//     private readonly ITokenService _tokenService = tokenService;

//     [HttpPost("login")]
//     public async Task<IActionResult> Login([FromBody] LdapLoginRequest request)
//     {
//         var passport = new UserPassport(request.Username, request.Domain, request.Password);

//         var result = await _ldap.AuthenticateAsync(passport);

//         if (!result.Success)
//             return Unauthorized(result.Error);

//         if (result.User is null)
//             return Unauthorized("LDAP returned no user information.");

//         LdapUser ldapUser = result.User;

//         if (string.IsNullOrWhiteSpace(ldapUser.OfficeLocation))
//             return BadRequest("LDAP user has no office location.");

//         string normalizedUserName = _userManager.NormalizeName(ldapUser.Username);
//         AuthServiceUser? user = await _userManager.Users.SingleOrDefaultAsync(u =>
//             u.NormalizedUserName == normalizedUserName
//         );

//         // Update or create Identity user
//         if (user == null)
//         {
//             user = AuthServiceUser.CreateFromLdap(ldapUser);

//             IdentityResult createResult = await _userManager.CreateAsync(user);

//             if (!createResult.Succeeded)
//                 return StatusCode(500, "Failed to create user.");
//         }
//         else
//         {
//             user.UpdateFromLdap(ldapUser);
//             await _userManager.UpdateAsync(user);
//         }

//         // Issue tokens via OpenIddict
//         return await _tokenService.CreateTokenAsync(user);
//     }
// }
