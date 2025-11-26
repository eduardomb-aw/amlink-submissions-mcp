using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace amlink_submissions_mcp_bff.Controllers;

/// <summary>
/// Protected endpoints controller for testing authentication behavior.
/// </summary>
[ApiController]
[Route("api/protected")]
[Authorize]
public class ProtectedController : ControllerBase
{
    /// <summary>
    /// Protected endpoint that returns user information.
    /// Requires authentication - returns 401 if not authenticated.
    /// </summary>
    /// <returns>User information for authenticated users</returns>
    [HttpGet("userinfo")]
    public IActionResult UserInfo()
    {
        // Let the [Authorize] attribute handle authentication challenges
        // This will automatically redirect to login if not authenticated
        return Ok(new
        {
            isAuthenticated = true,
            user = User.Identity?.Name ?? "test-user",
            email = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value ?? "test@example.com",
            timestamp = DateTime.UtcNow
        });
    }
}
