using Microsoft.EntityFrameworkCore;
using JavisApi.Models;

namespace JavisApi.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Source> Sources => Set<Source>();
    public DbSet<SourceDepartment> SourceDepartments => Set<SourceDepartment>();
    public DbSet<WikiPage> WikiPages => Set<WikiPage>();
    public DbSet<WikiLink> WikiLinks => Set<WikiLink>();
    public DbSet<WikiPageDraft> WikiPageDrafts => Set<WikiPageDraft>();
    public DbSet<WikiPageRevision> WikiPageRevisions => Set<WikiPageRevision>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<ProjectMember> ProjectMembers => Set<ProjectMember>();
    public DbSet<ProjectSource> ProjectSources => Set<ProjectSource>();
    public DbSet<KnowledgeType> KnowledgeTypes => Set<KnowledgeType>();
    public DbSet<Skill> Skills => Set<Skill>();
    public DbSet<SkillVersion> SkillVersions => Set<SkillVersion>();
    public DbSet<SkillDepartment> SkillDepartments => Set<SkillDepartment>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<AppConfig> AppConfigs => Set<AppConfig>();
    public DbSet<Note> Notes => Set<Note>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // --- Composite Primary Keys ---
        modelBuilder.Entity<SourceDepartment>()
            .HasKey(x => new { x.SourceId, x.DepartmentId });

        modelBuilder.Entity<WikiLink>()
            .HasKey(x => new { x.FromSlug, x.ToSlug });

        modelBuilder.Entity<ProjectMember>()
            .HasKey(x => new { x.ProjectId, x.EmployeeId });

        modelBuilder.Entity<ProjectSource>()
            .HasKey(x => new { x.ProjectId, x.SourceId });

        modelBuilder.Entity<SkillDepartment>()
            .HasKey(x => new { x.SkillId, x.DepartmentId });

        // --- Unique Constraints ---
        modelBuilder.Entity<Employee>()
            .HasIndex(x => x.Email).IsUnique();

        modelBuilder.Entity<Employee>()
            .HasIndex(x => x.McpToken).IsUnique();

        modelBuilder.Entity<Department>()
            .HasIndex(x => x.Name).IsUnique();

        modelBuilder.Entity<Role>()
            .HasIndex(x => x.Name).IsUnique();

        modelBuilder.Entity<KnowledgeType>()
            .HasIndex(x => x.Slug).IsUnique();

        modelBuilder.Entity<Skill>()
            .HasIndex(x => x.Slug).IsUnique();

        modelBuilder.Entity<WikiPage>()
            .HasIndex(x => new { x.Slug, x.ScopeType, x.ScopeId }).IsUnique();

        // --- Indexes ---
        modelBuilder.Entity<AuditLog>()
            .HasIndex(x => x.Timestamp);

        modelBuilder.Entity<AuditLog>()
            .HasIndex(x => x.PrincipalId);

        modelBuilder.Entity<WikiPageDraft>()
            .HasIndex(x => x.Status);

        modelBuilder.Entity<WikiPageRevision>()
            .HasIndex(x => new { x.PageId, x.Version });

        // --- Relationships: SourceDepartment ---
        modelBuilder.Entity<SourceDepartment>()
            .HasOne(sd => sd.Source)
            .WithMany(s => s.SourceDepartments)
            .HasForeignKey(sd => sd.SourceId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<SourceDepartment>()
            .HasOne(sd => sd.Department)
            .WithMany(d => d.SourceDepartments)
            .HasForeignKey(sd => sd.DepartmentId)
            .OnDelete(DeleteBehavior.Cascade);

        // --- Relationships: Department/Employee ---
        modelBuilder.Entity<Employee>()
            .HasOne(e => e.Department)
            .WithMany(d => d.Employees)
            .HasForeignKey(e => e.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Employee>()
            .HasOne(e => e.CustomRole)
            .WithMany(r => r.Employees)
            .HasForeignKey(e => e.CustomRoleId)
            .OnDelete(DeleteBehavior.SetNull);

        // --- Relationships: Project ---
        modelBuilder.Entity<ProjectMember>()
            .HasOne(pm => pm.Project)
            .WithMany(p => p.Members)
            .HasForeignKey(pm => pm.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ProjectMember>()
            .HasOne(pm => pm.Employee)
            .WithMany()
            .HasForeignKey(pm => pm.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ProjectSource>()
            .HasOne(ps => ps.Project)
            .WithMany(p => p.ProjectSources)
            .HasForeignKey(ps => ps.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ProjectSource>()
            .HasOne(ps => ps.Source)
            .WithMany()
            .HasForeignKey(ps => ps.SourceId)
            .OnDelete(DeleteBehavior.Cascade);

        // --- Relationships: Skill ---
        modelBuilder.Entity<SkillDepartment>()
            .HasOne(sd => sd.Skill)
            .WithMany(s => s.SkillDepartments)
            .HasForeignKey(sd => sd.SkillId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<SkillDepartment>()
            .HasOne(sd => sd.Department)
            .WithMany(d => d.SkillDepartments)
            .HasForeignKey(sd => sd.DepartmentId)
            .OnDelete(DeleteBehavior.Cascade);

        // --- Relationships: Wiki ---
        modelBuilder.Entity<WikiPageDraft>()
            .HasOne(d => d.Page)
            .WithMany()
            .HasForeignKey(d => d.PageId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<WikiPageDraft>()
            .HasOne(d => d.Author)
            .WithMany()
            .HasForeignKey(d => d.AuthorId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<WikiPageDraft>()
            .HasOne(d => d.Reviewer)
            .WithMany()
            .HasForeignKey(d => d.ReviewedById)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<WikiPageRevision>()
            .HasOne(r => r.Page)
            .WithMany()
            .HasForeignKey(r => r.PageId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<WikiPageRevision>()
            .HasOne(r => r.ChangedBy)
            .WithMany()
            .HasForeignKey(r => r.ChangedById)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
