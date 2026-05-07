using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using JavisApi.Data;
using JavisApi.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace JavisApi.Services;

public class AuthService
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;

    public AuthService(AppDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    public async Task<Employee?> ValidateCredentialsAsync(string email, string password)
    {
        var employee = await _db.Employees
            .Include(e => e.Department)
            .Include(e => e.CustomRole)
            .FirstOrDefaultAsync(e => e.Email == email && e.IsActive);

        if (employee is null || string.IsNullOrEmpty(employee.PasswordHash))
            return null;

        return BCrypt.Net.BCrypt.Verify(password, employee.PasswordHash) ? employee : null;
    }

    public string GenerateJwtToken(Employee employee)
    {
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_config["Jwt:SecretKey"]!));

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, employee.Id.ToString()),
            new Claim(ClaimTypes.Email, employee.Email),
            new Claim(ClaimTypes.Name, employee.Name),
            new Claim("role", employee.Role),
            new Claim("department_id", employee.DepartmentId.ToString()),
        };

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddDays(7),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256)
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string HashPassword(string password) =>
        BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);

    public bool VerifyPassword(string password, string hash) =>
        BCrypt.Net.BCrypt.Verify(password, hash);

    public string GenerateMcpToken() =>
        "ark_" + Convert.ToHexString(RandomNumberGenerator.GetBytes(16)).ToLowerInvariant();

    public async Task<Employee?> GetByIdAsync(Guid id) =>
        await _db.Employees
            .Include(e => e.Department)
            .Include(e => e.CustomRole)
            .FirstOrDefaultAsync(e => e.Id == id && e.IsActive);

    public async Task<bool> ChangePasswordAsync(Guid employeeId, string currentPassword, string newPassword)
    {
        var employee = await _db.Employees.FindAsync(employeeId);
        if (employee is null || string.IsNullOrEmpty(employee.PasswordHash))
            return false;

        if (!VerifyPassword(currentPassword, employee.PasswordHash))
            return false;

        employee.PasswordHash = HashPassword(newPassword);
        employee.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return true;
    }
}
