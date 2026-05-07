using System.Security.Claims;
using JavisApi.Data;
using JavisApi.DTOs.Projects;
using JavisApi.Models;
using JavisApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JavisApi.Controllers;

[ApiController]
[Route("api/projects")]
[Authorize]
public class ProjectsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly PermissionEngine _permissions;
    private readonly AuditService _audit;

    public ProjectsController(AppDbContext db, PermissionEngine permissions, AuditService audit)
    {
        _db = db;
        _permissions = permissions;
        _audit = audit;
    }

    [HttpGet]
    public async Task<IActionResult> List()
    {
        var employee = await GetEmployeeAsync();
        if (employee is null) return Unauthorized();

        var query = _db.Projects
            .Include(p => p.Members)
            .Include(p => p.CreatedBy)
            .AsQueryable();

        if (!_permissions.IsAdmin(employee))
            query = query.Where(p => p.Members.Any(m => m.EmployeeId == employee.Id));

        var projects = await query
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new ProjectDto(
                p.Id, p.Name, p.Description, p.WorkspaceType, p.Status,
                p.CreatedById, p.CreatedBy != null ? p.CreatedBy.Name : null,
                p.Members.Count, p.CreatedAt, p.UpdatedAt))
            .ToListAsync();

        return Ok(projects);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id)
    {
        var employee = await GetEmployeeAsync();
        if (employee is null) return Unauthorized();

        var project = await _db.Projects
            .Include(p => p.Members)
            .Include(p => p.CreatedBy)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (project is null) return NotFound();
        if (!await _permissions.CanViewProjectAsync(employee, id)) return Forbid();

        return Ok(new ProjectDto(
            project.Id, project.Name, project.Description,
            project.WorkspaceType, project.Status,
            project.CreatedById, project.CreatedBy?.Name,
            project.Members.Count, project.CreatedAt, project.UpdatedAt));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateProjectRequest req)
    {
        var employee = await GetEmployeeAsync();
        if (employee is null) return Unauthorized();

        var project = new Project
        {
            Name = req.Name,
            Description = req.Description,
            WorkspaceType = req.WorkspaceType,
            CreatedById = employee.Id
        };
        _db.Projects.Add(project);

        // Creator becomes admin of the project
        project.Members.Add(new ProjectMember
        {
            ProjectId = project.Id,
            EmployeeId = employee.Id,
            Role = "admin"
        });

        await _db.SaveChangesAsync();
        await _audit.LogAsync(employee.Id, "create", "project", project.Id.ToString());

        return CreatedAtAction(nameof(Get), new { id = project.Id },
            new ProjectDto(project.Id, project.Name, project.Description,
                project.WorkspaceType, project.Status,
                project.CreatedById, null, 1, project.CreatedAt, project.UpdatedAt));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateProjectRequest req)
    {
        var employee = await GetEmployeeAsync();
        if (employee is null) return Unauthorized();
        if (!await _permissions.CanEditProjectAsync(employee, id)) return Forbid();

        var project = await _db.Projects.FindAsync(id);
        if (project is null) return NotFound();

        if (req.Name is not null) project.Name = req.Name;
        if (req.Description is not null) project.Description = req.Description;
        if (req.Status is not null) project.Status = req.Status;
        project.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return Ok(new { message = "Updated" });
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var employee = await GetEmployeeAsync();
        if (employee is null) return Unauthorized();
        if (!await _permissions.CanAdminProjectAsync(employee, id)) return Forbid();

        var project = await _db.Projects.FindAsync(id);
        if (project is null) return NotFound();

        _db.Projects.Remove(project);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // -----------------------------------------------------------------------
    // Members
    // -----------------------------------------------------------------------

    [HttpGet("{id:guid}/members")]
    public async Task<IActionResult> GetMembers(Guid id)
    {
        var employee = await GetEmployeeAsync();
        if (employee is null) return Unauthorized();
        if (!await _permissions.CanViewProjectAsync(employee, id)) return Forbid();

        var members = await _db.ProjectMembers
            .Include(m => m.Employee)
            .ThenInclude(e => e!.Department)
            .Where(m => m.ProjectId == id)
            .Select(m => new ProjectMemberDto(
                m.EmployeeId,
                m.Employee != null ? m.Employee.Name : "",
                m.Employee != null ? m.Employee.Email : "",
                m.Employee != null && m.Employee.Department != null ? m.Employee.Department.Name : null,
                m.Role, m.AddedAt))
            .ToListAsync();

        return Ok(members);
    }

    [HttpPost("{id:guid}/members")]
    public async Task<IActionResult> AddMember(Guid id, [FromBody] AddMemberRequest req)
    {
        var employee = await GetEmployeeAsync();
        if (employee is null) return Unauthorized();
        if (!await _permissions.CanAdminProjectAsync(employee, id)) return Forbid();

        var exists = await _db.ProjectMembers
            .AnyAsync(m => m.ProjectId == id && m.EmployeeId == req.EmployeeId);

        if (exists) return Conflict(new { message = "Member already exists" });

        _db.ProjectMembers.Add(new ProjectMember
        {
            ProjectId = id,
            EmployeeId = req.EmployeeId,
            Role = req.Role
        });

        await _db.SaveChangesAsync();
        return Ok(new { message = "Member added" });
    }

    [HttpPut("{id:guid}/members/{employeeId:guid}")]
    public async Task<IActionResult> UpdateMemberRole(Guid id, Guid employeeId,
        [FromBody] UpdateMemberRoleRequest req)
    {
        var employee = await GetEmployeeAsync();
        if (employee is null) return Unauthorized();
        if (!await _permissions.CanAdminProjectAsync(employee, id)) return Forbid();

        var member = await _db.ProjectMembers
            .FirstOrDefaultAsync(m => m.ProjectId == id && m.EmployeeId == employeeId);

        if (member is null) return NotFound();
        member.Role = req.Role;
        await _db.SaveChangesAsync();
        return Ok(new { message = "Role updated" });
    }

    [HttpDelete("{id:guid}/members/{employeeId:guid}")]
    public async Task<IActionResult> RemoveMember(Guid id, Guid employeeId)
    {
        var employee = await GetEmployeeAsync();
        if (employee is null) return Unauthorized();
        if (!await _permissions.CanAdminProjectAsync(employee, id)) return Forbid();

        var member = await _db.ProjectMembers
            .FirstOrDefaultAsync(m => m.ProjectId == id && m.EmployeeId == employeeId);

        if (member is null) return NotFound();
        _db.ProjectMembers.Remove(member);
        await _db.SaveChangesAsync();
        return NoContent();
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
