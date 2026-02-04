using AuthService.Clients.LdapClient;
using AuthService.Models.Dto.Miscellaneous;
using Microsoft.AspNetCore.Mvc;

namespace AuthService.Controllers;

[ApiController]
[ApiExplorerSettings(GroupName = "miscellaneous")]
[Route("api/miscellaneous")]
public class MiscellaneousController(LdapConfig ldapConfig) : ControllerBase
{
    private readonly LdapConfig _ldapConfig = ldapConfig;

    // --------------------------------------------------------------
    // Get allowed EMEA office names
    // --------------------------------------------------------------
    [HttpGet("emea-offices")]
    public ActionResult<GetAllowedEmeaOfficesResponseDto> GetAllowedEmeaOffices()
    {
        return Ok(new GetAllowedEmeaOfficesResponseDto(_ldapConfig.AllowedEmeaOfficeNames));
    }
}
