using System.Security.Claims;
using JavisApi.Data;
using JavisApi.DTOs.Employees;
using JavisApi.Models;
using JavisApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JavisApi.Controllers;

[ApiController]
[Route("api")]
[Authorize]
public class RbacController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly AuthService _auth;
    private readonly PermissionEngine _permissions;

    public RbacController(AppDbContext db, AuthService auth, PermissionEngine permissions)
    {
        _db = db;
        _auth = auth;
        _permissions = permissions;
    }

    // -----------------------------------------------------------------------
    // Departments
    // -----------------------------------------------------------------------

    [HttpGet("departments")]
    public async Task<IActionResult> ListDepartments()
    {
        var depts = await _db.Departments
            .Select(d => new DepartmentDto(
                d.Id, d.Name, d.Description,
                d.Employees.Count, d.CreatedAt))
            .OrderBy(d => d.Name)
            .ToListAsync();

        return Ok(depts);
    }

    [HttpPost("departments")]
    public async Task<IActionResult> CreateDepartment([FromBody] CreateDepartmentRequest req)
    {
        var employee = await GetEmployeeAsync();
        if (employee is null) return Unauthorized();
        if (!_permissions.CanManageOrg(employee)) return Forbid();

        var dept = new Department { Name = req.Name, Description = req.Description };
        _db.Departments.Add(dept);
        await _db.SaveChangesAsync();

        return Ok(new DepartmentDto(dept.Id, dept.Name, dept.Description, 0, dept.CreatedAt));
    }

    [HttpPut("departments/{id:guid}")]
    public async Task<IActionResult> UpdateDepartment(Guid id, [FromBody] UpdateDepartmentRequest req)
    {
        var employee = await GetEmployeeAsync();
        if (employee is null) return Unauthorized();
        if (!_permissions.CanManageOrg(employee)) return Forbid();

        var dept = await _db.Departments.FindAsync(id);
        if (dept is null) return NotFound();

        if (req.Name is not null) dept.Name = req.Name;
        if (req.Description is not null) dept.Description = req.Description;
        dept.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return Ok(new { message = "Updated" });
    }

    [HttpDelete("departments/{id:guid}")]
    public async Task<IActionResult> DeleteDepartment(Guid id)
    {
        var employee = await GetEmployeeAsync();
        if (employee is null) return Unauthorized();
        if (!_permissions.IsAdmin(employee)) return Forbid();

        var dept = await _db.Departments.FindAsync(id);
        if (dept is null) return NotFound();

        _db.Departments.Remove(dept);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // -----------------------------------------------------------------------
    // Employees
    // -----------------------------------------------------------------------

    [HttpGet("employees")]
    public async Task<IActionResult> ListEmployees()
    {
        var currentEmployee = await GetEmployeeAsync();
        if (currentEmployee is null) return Unauthorized();
        if (!_permissions.CanManageOrg(currentEmployee)) return Forbid();

        var employees = await _db.Employees
            .Include(e => e.Department)
            .Include(e => e.CustomRole)
            .Select(e => new
            {
                e.Id, e.Name, e.Email, e.Role,
                e.DepartmentId,
                DepartmentName = e.Department != null ? e.Department.Name : null,
                e.CustomRoleId,
                CustomRoleName = e.CustomRole != null ? e.CustomRole.Name : null,
                e.McpToken, e.IsActive,
                e.LastConnected, e.CreatedAt
            })
            .OrderBy(e => e.Name)
            .ToListAsync();

        return Ok(employees);
    }

    [HttpPost("employees")]
    public async Task<IActionResult> CreateEmployee([FromBody] CreateEmployeeRequest req)
    {
        var currentEmployee = await GetEmployeeAsync();
        if (currentEmployee is null) return Unauthorized();
        if (!_permissions.CanManageOrg(currentEmployee)) return Forbid();

        var exists = await _db.Employees.AnyAsync(e => e.Email == req.Email);
        if (exists) return Conflict(new { message = "Email already in use" });

        var employee = new Employee
        {
            Name = req.Name,
            Email = req.Email,
            PasswordHash = _auth.HashPassword(req.Password),
            Role = req.Role,
            DepartmentId = req.DepartmentId,
            CustomRoleId = req.CustomRoleId
        };

        _db.Employees.Add(employee);
        await _db.SaveChangesAsync();
        return Ok(new { employee.Id, employee.Name, employee.Email });
    }

    [HttpPut("employees/{id:guid}")]
    public async Task<IActionResult> UpdateEmployee(Guid id, [FromBody] UpdateEmployeeRequest req)
    {
        var currentEmployee = await GetEmployeeAsync();
        if (currentEmployee is null) return Unauthorized();
        if (!_permissions.CanManageOrg(currentEmployee)) return Forbid();

        var emp = await _db.Employees.FindAsync(id);
        if (emp is null) return NotFound();

        if (req.Name is not null) emp.Name = req.Name;
        if (req.Email is not null) emp.Email = req.Email;
        if (req.DepartmentId.HasValue) emp.DepartmentId = req.DepartmentId.Value;
        if (req.Role is not null) emp.Role = req.Role;
        if (req.CustomRoleId is not null) emp.CustomRoleId = req.CustomRoleId;
        if (req.IsActive.HasValue) emp.IsActive = req.IsActive.Value;
        emp.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return Ok(new { message = "Updated" });
    }

    [HttpDelete("employees/{id:guid}")]
    public async Task<IActionResult> DeleteEmployee(Guid id)
    {
        var currentEmployee = await GetEmployeeAsync();
        if (currentEmployee is null) return Unauthorized();
        if (!_permissions.IsAdmin(currentEmployee)) return Forbid();
        if (id == currentEmployee.Id) return BadRequest(new { message = "Cannot delete yourself" });

        var emp = await _db.Employees.FindAsync(id);
        if (emp is null) return NotFound();

        _db.Employees.Remove(emp);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("employees/{id:guid}/mcp-token")]
    public async Task<IActionResult> GenerateMcpToken(Guid id)
    {
        var currentEmployee = await GetEmployeeAsync();
        if (currentEmployee is null) return Unauthorized();
        if (!_permissions.CanManageOrg(currentEmployee) && currentEmployee.Id != id) return Forbid();

        var emp = await _db.Employees.FindAsync(id);
        if (emp is null) return NotFound();

        emp.McpToken = _auth.GenerateMcpToken();
        emp.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return Ok(new { mcpToken = emp.McpToken });
    }

    // -----------------------------------------------------------------------
    // Roles
    // -----------------------------------------------------------------------

    [HttpGet("roles")]
    public async Task<IActionResult> ListRoles()
    {
        var roles = await _db.Roles
            .Select(r => new RoleDto(
                r.Id, r.Name, r.Description,
                r.Employees.Select(e => e.Id).Any()
                    ? new List<string>() : new List<string>(),
                r.IsSystem,
                r.Employees.Count,
                r.CreatedAt))
            .ToListAsync();

        // Load permissions separately (not mapped by EF)
        var rawRoles = await _db.Roles.ToListAsync();
        return Ok(rawRoles.Select(r => new RoleDto(
            r.Id, r.Name, r.Description, r.Permissions, r.IsSystem,
            0, r.CreatedAt)));
    }

    [HttpPost("roles")]
    public async Task<IActionResult> CreateRole([FromBody] CreateRoleRequest req)
    {
        var employee = await GetEmployeeAsync();
        if (employee is null) return Unauthorized();
        if (!_permissions.CanManageOrg(employee)) return Forbid();

        var role = new Role { Name = req.Name, Description = req.Description };
        if (req.Permissions is not null) role.Permissions = req.Permissions;

        _db.Roles.Add(role);
        await _db.SaveChangesAsync();

        return Ok(new RoleDto(role.Id, role.Name, role.Description,
            role.Permissions, role.IsSystem, 0, role.CreatedAt));
    }

    [HttpPut("roles/{id:guid}")]
    public async Task<IActionResult> UpdateRole(Guid id, [FromBody] UpdateRoleRequest req)
    {
        var employee = await GetEmployeeAsync();
        if (employee is null) return Unauthorized();
        if (!_permissions.CanManageOrg(employee)) return Forbid();

        var role = await _db.Roles.FindAsync(id);
        if (role is null) return NotFound();
        if (role.IsSystem) return BadRequest(new { message = "System roles cannot be modified" });

        if (req.Name is not null) role.Name = req.Name;
        if (req.Description is not null) role.Description = req.Description;
        if (req.Permissions is not null) role.Permissions = req.Permissions;
        role.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return Ok(new { message = "Updated" });
    }

    [HttpDelete("roles/{id:guid}")]
    public async Task<IActionResult> DeleteRole(Guid id)
    {
        var employee = await GetEmployeeAsync();
        if (employee is null) return Unauthorized();
        if (!_permissions.IsAdmin(employee)) return Forbid();

        var role = await _db.Roles.FindAsync(id);
        if (role is null) return NotFound();
        if (role.IsSystem) return BadRequest(new { message = "Cannot delete system roles" });

        _db.Roles.Remove(role);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // -----------------------------------------------------------------------
    // Knowledge Types
    // -----------------------------------------------------------------------

    [HttpGet("knowledge-types")]
    public async Task<IActionResult> ListKnowledgeTypes()
    {
        var types = await _db.KnowledgeTypes
            .OrderBy(k => k.SortOrder).ThenBy(k => k.Name)
            .Select(k => new KnowledgeTypeDto(k.Id, k.Slug, k.Name, k.Color, k.Description, k.SortOrder))
            .ToListAsync();

        return Ok(types);
    }

    [HttpPost("knowledge-types")]
    public async Task<IActionResult> CreateKnowledgeType([FromBody] CreateKnowledgeTypeRequest req)
    {
        var employee = await GetEmployeeAsync();
        if (employee is null) return Unauthorized();
        if (!_permissions.CanManageOrg(employee)) return Forbid();

        var kt = new KnowledgeType
        {
            Slug = req.Slug.ToLower(),
            Name = req.Name,
            Color = req.Color,
            Description = req.Description,
            SortOrder = req.SortOrder
        };

        _db.KnowledgeTypes.Add(kt);
        await _db.SaveChangesAsync();
        return Ok(new KnowledgeTypeDto(kt.Id, kt.Slug, kt.Name, kt.Color, kt.Description, kt.SortOrder));
    }

    private async Task<Employee?> GetEmployeeAsync()
    {
        var idStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(idStr, out var id)) return null;
        return await _db.Employees
            .Include(e => e.CustomRole)
            .FirstOrDefaultAsync(e => e.Id == id && e.IsActive);
    }
}
