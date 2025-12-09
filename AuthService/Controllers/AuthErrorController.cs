using AuthService.Data;
using AuthService.Models;
using AuthService.Models.Dto.Errors;
using Microsoft.AspNetCore.Mvc;

namespace AuthService.Controllers;

[ApiController]
[ApiExplorerSettings(IgnoreApi = true)]
[Route("api/auth-error")]
public class AuthErrorController(AuthServiceDbContext db) : ControllerBase
{
    private readonly AuthServiceDbContext _db = db;

    /// <summary>
    /// Stores a frontend-reported error with a reference UUID and full details.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] AuthErrorRequestDto req)
    {
        var entry = new AuthError { Reference = req.Reference, ErrorDetails = req.ErrorDetails };

        _db.AuthErrors.Add(entry);
        await _db.SaveChangesAsync();

        return Ok();
    }
}
