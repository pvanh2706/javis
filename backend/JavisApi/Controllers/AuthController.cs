using System.Security.Claims;
using JavisApi.Data;
using JavisApi.DTOs.Auth;
using JavisApi.Models;
using JavisApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JavisApi.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AuthService _auth;
    private readonly AppDbContext _db;

    public AuthController(AuthService auth, AppDbContext db)
    {
        _auth = auth;
        _db = db;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest req)
    {
        var employee = await _auth.ValidateCredentialsAsync(req.Email, req.Password);
        if (employee is null)
            return Unauthorized(new { message = "Invalid email or password" });

        var token = _auth.GenerateJwtToken(employee);
        var dto = MapToDto(employee);

        return Ok(new LoginResponse(token, "bearer", dto));
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> Me()
    {
        var id = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var employee = await _auth.GetByIdAsync(id);
        if (employee is null) return Unauthorized();

        return Ok(MapToDto(employee));
    }

    [Authorize]
    [HttpPut("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest req)
    {
        var id = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var success = await _auth.ChangePasswordAsync(id, req.CurrentPassword, req.NewPassword);

        return success
            ? Ok(new { message = "Password changed successfully" })
            : BadRequest(new { message = "Current password is incorrect" });
    }

    private static EmployeeDto MapToDto(Employee e) => new(
        e.Id, e.Name, e.Email, e.Role,
        e.DepartmentId, e.Department?.Name,
        e.CustomRoleId, e.McpToken,
        e.IsActive, e.CreatedAt);
}
