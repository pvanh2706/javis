using JavisApi.Data;
using JavisApi.Models;
using Microsoft.EntityFrameworkCore;

namespace JavisApi.Services;

/// <summary>
/// Dual-realm permission engine.
/// Realm 1: Global — permission strings on custom roles (doc:read:all, wiki:edit:own_dept...)
/// Realm 2: Workspace — membership-gated (viewer/contributor/editor/admin)
/// Admin role bypasses all checks.
/// </summary>
public class PermissionEngine
{
    private readonly AppDbContext _db;

    public PermissionEngine(AppDbContext db)
    {
        _db = db;
    }

    // -----------------------------------------------------------------------
    // Global Realm
    // -----------------------------------------------------------------------

    public bool IsAdmin(Employee employee) => employee.Role == "admin";

    public bool HasPermission(Employee employee, string permission)
    {
        if (IsAdmin(employee)) return true;
        if (employee.CustomRole is null) return false;

        var perms = employee.CustomRole.Permissions;
        // Check exact match or wildcard
        return perms.Contains(permission) || perms.Contains("*");
    }

    public bool CanReadSources(Employee employee) =>
        IsAdmin(employee) ||
        HasPermission(employee, "doc:read:all") ||
        HasPermission(employee, "doc:read:own_dept");

    public bool CanUploadSource(Employee employee) =>
        IsAdmin(employee) || HasPermission(employee, "doc:upload");

    public bool CanDeleteSource(Employee employee) =>
        IsAdmin(employee) || HasPermission(employee, "doc:delete");

    public bool CanReadWiki(Employee employee) =>
        IsAdmin(employee) ||
        HasPermission(employee, "wiki:read:all") ||
        HasPermission(employee, "wiki:read:own_dept");

    public bool CanEditWiki(Employee employee) =>
        IsAdmin(employee) || HasPermission(employee, "wiki:edit:all");

    public bool CanManageOrg(Employee employee) =>
        IsAdmin(employee) || HasPermission(employee, "org:settings:manage");

    /// <summary>
    /// Returns the department IDs an employee can see sources for.
    /// If "doc:read:all" → null (no filter, see all).
    /// If "doc:read:own_dept" → [employee.DepartmentId].
    /// </summary>
    public List<Guid>? GetAccessibleDepartmentIds(Employee employee)
    {
        if (IsAdmin(employee)) return null;
        if (HasPermission(employee, "doc:read:all")) return null;
        if (HasPermission(employee, "doc:read:own_dept"))
            return [employee.DepartmentId];
        return [];
    }

    // -----------------------------------------------------------------------
    // Workspace Realm
    // -----------------------------------------------------------------------

    public async Task<string?> GetWorkspaceRoleAsync(Guid employeeId, Guid projectId)
    {
        var member = await _db.ProjectMembers
            .FirstOrDefaultAsync(m => m.ProjectId == projectId && m.EmployeeId == employeeId);
        return member?.Role;
    }

    public async Task<bool> CanViewProjectAsync(Employee employee, Guid projectId)
    {
        if (IsAdmin(employee)) return true;
        var role = await GetWorkspaceRoleAsync(employee.Id, projectId);
        return role is not null;
    }

    public async Task<bool> CanContributeToProjectAsync(Employee employee, Guid projectId)
    {
        if (IsAdmin(employee)) return true;
        var role = await GetWorkspaceRoleAsync(employee.Id, projectId);
        return role is "contributor" or "editor" or "admin";
    }

    public async Task<bool> CanEditProjectAsync(Employee employee, Guid projectId)
    {
        if (IsAdmin(employee)) return true;
        var role = await GetWorkspaceRoleAsync(employee.Id, projectId);
        return role is "editor" or "admin";
    }

    public async Task<bool> CanAdminProjectAsync(Employee employee, Guid projectId)
    {
        if (IsAdmin(employee)) return true;
        var role = await GetWorkspaceRoleAsync(employee.Id, projectId);
        return role == "admin";
    }

    // -----------------------------------------------------------------------
    // Source visibility filter
    // -----------------------------------------------------------------------

    /// <summary>
    /// Filter sources query by employee's permission scope.
    /// A source with NO department rows is global (visible to all).
    /// A source WITH department rows is visible only to employees in those departments.
    /// </summary>
    public IQueryable<Source> FilterSources(IQueryable<Source> query, Employee employee)
    {
        if (IsAdmin(employee)) return query;

        var accessibleDepts = GetAccessibleDepartmentIds(employee);
        if (accessibleDepts is null) return query; // all

        if (accessibleDepts.Count == 0)
            return query.Where(s => false); // no access

        // Visible if: no department restriction OR employee's dept is in the list
        return query.Where(s =>
            !s.SourceDepartments.Any() ||
            s.SourceDepartments.Any(sd => accessibleDepts.Contains(sd.DepartmentId)));
    }

    // -----------------------------------------------------------------------
    // Wiki visibility filter
    // -----------------------------------------------------------------------

    /// <summary>
    /// Filter wiki pages by employee's scope access.
    /// Global pages: visible to all authenticated users.
    /// Project pages: visible only to project members.
    /// </summary>
    public async Task<IQueryable<WikiPage>> FilterWikiPagesAsync(
        IQueryable<WikiPage> query, Employee employee)
    {
        if (IsAdmin(employee)) return query;

        // Get employee's project memberships
        var projectIds = await _db.ProjectMembers
            .Where(m => m.EmployeeId == employee.Id)
            .Select(m => m.ProjectId)
            .ToListAsync();

        return query.Where(p =>
            p.ScopeType == "global" ||
            (p.ScopeType == "project" && p.ScopeId.HasValue && projectIds.Contains(p.ScopeId.Value)));
    }
}
