using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JavisApi.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "app_config",
                columns: table => new
                {
                    key = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    value = table.Column<string>(type: "TEXT", nullable: true),
                    updated_at = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_app_config", x => x.key);
                });

            migrationBuilder.CreateTable(
                name: "audit_log",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "TEXT", nullable: false),
                    timestamp = table.Column<DateTime>(type: "TEXT", nullable: false),
                    principal_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    principal_type = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    action = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    resource_type = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    resource_id = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    decision = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    reason = table.Column<string>(type: "TEXT", nullable: true),
                    metadata_json = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_audit_log", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "departments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "TEXT", nullable: false),
                    name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "TEXT", nullable: true),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    updated_at = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_departments", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "knowledge_types",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "TEXT", nullable: false),
                    slug = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    color = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    description = table.Column<string>(type: "TEXT", nullable: true),
                    sort_order = table.Column<int>(type: "INTEGER", nullable: false),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_knowledge_types", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "notes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "TEXT", nullable: false),
                    title = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    content = table.Column<string>(type: "TEXT", nullable: true),
                    note_type = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    updated_at = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notes", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "roles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "TEXT", nullable: false),
                    name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "TEXT", nullable: true),
                    permissions = table.Column<string>(type: "TEXT", nullable: false),
                    is_system = table.Column<bool>(type: "INTEGER", nullable: false),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    updated_at = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_roles", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "skills",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "TEXT", nullable: false),
                    name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    slug = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "TEXT", nullable: true),
                    scope_type = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    scope_id = table.Column<Guid>(type: "TEXT", nullable: true),
                    current_version = table.Column<int>(type: "INTEGER", nullable: false),
                    version_hash = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    storage_path = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    updated_at = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_skills", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "wiki_links",
                columns: table => new
                {
                    from_slug = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                    to_slug = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_wiki_links", x => new { x.from_slug, x.to_slug });
                });

            migrationBuilder.CreateTable(
                name: "wiki_pages",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "TEXT", nullable: false),
                    slug = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                    title = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    page_type = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    content_md = table.Column<string>(type: "TEXT", nullable: false),
                    summary = table.Column<string>(type: "TEXT", nullable: false),
                    scope_type = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    scope_id = table.Column<Guid>(type: "TEXT", nullable: true),
                    knowledge_type_slugs_json = table.Column<string>(type: "TEXT", nullable: false),
                    source_ids_json = table.Column<string>(type: "TEXT", nullable: false),
                    version = table.Column<int>(type: "INTEGER", nullable: false),
                    orphaned = table.Column<bool>(type: "INTEGER", nullable: false),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    updated_at = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_wiki_pages", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "employees",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "TEXT", nullable: false),
                    name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    email = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    password_hash = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    role = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    department_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    custom_role_id = table.Column<Guid>(type: "TEXT", nullable: true),
                    mcp_token = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    is_active = table.Column<bool>(type: "INTEGER", nullable: false),
                    last_connected = table.Column<DateTime>(type: "TEXT", nullable: true),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    updated_at = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_employees", x => x.id);
                    table.ForeignKey(
                        name: "FK_employees_departments_department_id",
                        column: x => x.department_id,
                        principalTable: "departments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_employees_roles_custom_role_id",
                        column: x => x.custom_role_id,
                        principalTable: "roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "skill_departments",
                columns: table => new
                {
                    skill_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    department_id = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_skill_departments", x => new { x.skill_id, x.department_id });
                    table.ForeignKey(
                        name: "FK_skill_departments_departments_department_id",
                        column: x => x.department_id,
                        principalTable: "departments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_skill_departments_skills_skill_id",
                        column: x => x.skill_id,
                        principalTable: "skills",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "projects",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "TEXT", nullable: false),
                    name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "TEXT", nullable: true),
                    workspace_type = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    created_by_id = table.Column<Guid>(type: "TEXT", nullable: true),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    updated_at = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_projects", x => x.id);
                    table.ForeignKey(
                        name: "FK_projects_employees_created_by_id",
                        column: x => x.created_by_id,
                        principalTable: "employees",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "skill_versions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "TEXT", nullable: false),
                    skill_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    version_number = table.Column<int>(type: "INTEGER", nullable: false),
                    version_hash = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    storage_path = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    changelog = table.Column<string>(type: "TEXT", nullable: true),
                    created_by = table.Column<Guid>(type: "TEXT", nullable: true),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_skill_versions", x => x.id);
                    table.ForeignKey(
                        name: "FK_skill_versions_employees_created_by",
                        column: x => x.created_by,
                        principalTable: "employees",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_skill_versions_skills_skill_id",
                        column: x => x.skill_id,
                        principalTable: "skills",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "sources",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "TEXT", nullable: false),
                    title = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    full_text = table.Column<string>(type: "TEXT", nullable: true),
                    source_type = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    scope_type = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    scope_id = table.Column<Guid>(type: "TEXT", nullable: true),
                    knowledge_type_id = table.Column<Guid>(type: "TEXT", nullable: true),
                    contributed_by_employee_id = table.Column<Guid>(type: "TEXT", nullable: true),
                    file_path = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    url = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    minio_key = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    file_name = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    file_size = table.Column<long>(type: "INTEGER", nullable: true),
                    status = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    error_message = table.Column<string>(type: "TEXT", nullable: true),
                    progress = table.Column<int>(type: "INTEGER", nullable: false),
                    progress_message = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    job_id = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    outline_json = table.Column<string>(type: "TEXT", nullable: true),
                    page_offsets_json = table.Column<string>(type: "TEXT", nullable: true),
                    metadata_json = table.Column<string>(type: "TEXT", nullable: true),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    updated_at = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sources", x => x.id);
                    table.ForeignKey(
                        name: "FK_sources_employees_contributed_by_employee_id",
                        column: x => x.contributed_by_employee_id,
                        principalTable: "employees",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_sources_knowledge_types_knowledge_type_id",
                        column: x => x.knowledge_type_id,
                        principalTable: "knowledge_types",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "wiki_page_drafts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "TEXT", nullable: false),
                    page_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    author_id = table.Column<Guid>(type: "TEXT", nullable: true),
                    content_md = table.Column<string>(type: "TEXT", nullable: false),
                    note = table.Column<string>(type: "TEXT", nullable: true),
                    status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    source = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    source_metadata_json = table.Column<string>(type: "TEXT", nullable: true),
                    reviewed_by_id = table.Column<Guid>(type: "TEXT", nullable: true),
                    reviewed_at = table.Column<DateTime>(type: "TEXT", nullable: true),
                    reviewer_note = table.Column<string>(type: "TEXT", nullable: true),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    updated_at = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_wiki_page_drafts", x => x.id);
                    table.ForeignKey(
                        name: "FK_wiki_page_drafts_employees_author_id",
                        column: x => x.author_id,
                        principalTable: "employees",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_wiki_page_drafts_employees_reviewed_by_id",
                        column: x => x.reviewed_by_id,
                        principalTable: "employees",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_wiki_page_drafts_wiki_pages_page_id",
                        column: x => x.page_id,
                        principalTable: "wiki_pages",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "wiki_page_revisions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "TEXT", nullable: false),
                    page_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    version = table.Column<int>(type: "INTEGER", nullable: false),
                    content_md = table.Column<string>(type: "TEXT", nullable: false),
                    change_type = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    draft_id = table.Column<Guid>(type: "TEXT", nullable: true),
                    changed_by_id = table.Column<Guid>(type: "TEXT", nullable: true),
                    change_note = table.Column<string>(type: "TEXT", nullable: true),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_wiki_page_revisions", x => x.id);
                    table.ForeignKey(
                        name: "FK_wiki_page_revisions_employees_changed_by_id",
                        column: x => x.changed_by_id,
                        principalTable: "employees",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_wiki_page_revisions_wiki_pages_page_id",
                        column: x => x.page_id,
                        principalTable: "wiki_pages",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "project_members",
                columns: table => new
                {
                    project_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    employee_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    role = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    added_at = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_project_members", x => new { x.project_id, x.employee_id });
                    table.ForeignKey(
                        name: "FK_project_members_employees_employee_id",
                        column: x => x.employee_id,
                        principalTable: "employees",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_project_members_projects_project_id",
                        column: x => x.project_id,
                        principalTable: "projects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "project_sources",
                columns: table => new
                {
                    project_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    source_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    added_at = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_project_sources", x => new { x.project_id, x.source_id });
                    table.ForeignKey(
                        name: "FK_project_sources_projects_project_id",
                        column: x => x.project_id,
                        principalTable: "projects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_project_sources_sources_source_id",
                        column: x => x.source_id,
                        principalTable: "sources",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "source_departments",
                columns: table => new
                {
                    source_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    department_id = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_source_departments", x => new { x.source_id, x.department_id });
                    table.ForeignKey(
                        name: "FK_source_departments_departments_department_id",
                        column: x => x.department_id,
                        principalTable: "departments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_source_departments_sources_source_id",
                        column: x => x.source_id,
                        principalTable: "sources",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_audit_log_principal_id",
                table: "audit_log",
                column: "principal_id");

            migrationBuilder.CreateIndex(
                name: "IX_audit_log_timestamp",
                table: "audit_log",
                column: "timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_departments_name",
                table: "departments",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_employees_custom_role_id",
                table: "employees",
                column: "custom_role_id");

            migrationBuilder.CreateIndex(
                name: "IX_employees_department_id",
                table: "employees",
                column: "department_id");

            migrationBuilder.CreateIndex(
                name: "IX_employees_email",
                table: "employees",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_employees_mcp_token",
                table: "employees",
                column: "mcp_token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_knowledge_types_slug",
                table: "knowledge_types",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_project_members_employee_id",
                table: "project_members",
                column: "employee_id");

            migrationBuilder.CreateIndex(
                name: "IX_project_sources_source_id",
                table: "project_sources",
                column: "source_id");

            migrationBuilder.CreateIndex(
                name: "IX_projects_created_by_id",
                table: "projects",
                column: "created_by_id");

            migrationBuilder.CreateIndex(
                name: "IX_roles_name",
                table: "roles",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_skill_departments_department_id",
                table: "skill_departments",
                column: "department_id");

            migrationBuilder.CreateIndex(
                name: "IX_skill_versions_created_by",
                table: "skill_versions",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "IX_skill_versions_skill_id",
                table: "skill_versions",
                column: "skill_id");

            migrationBuilder.CreateIndex(
                name: "IX_skills_slug",
                table: "skills",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_source_departments_department_id",
                table: "source_departments",
                column: "department_id");

            migrationBuilder.CreateIndex(
                name: "IX_sources_contributed_by_employee_id",
                table: "sources",
                column: "contributed_by_employee_id");

            migrationBuilder.CreateIndex(
                name: "IX_sources_knowledge_type_id",
                table: "sources",
                column: "knowledge_type_id");

            migrationBuilder.CreateIndex(
                name: "IX_wiki_page_drafts_author_id",
                table: "wiki_page_drafts",
                column: "author_id");

            migrationBuilder.CreateIndex(
                name: "IX_wiki_page_drafts_page_id",
                table: "wiki_page_drafts",
                column: "page_id");

            migrationBuilder.CreateIndex(
                name: "IX_wiki_page_drafts_reviewed_by_id",
                table: "wiki_page_drafts",
                column: "reviewed_by_id");

            migrationBuilder.CreateIndex(
                name: "IX_wiki_page_drafts_status",
                table: "wiki_page_drafts",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "IX_wiki_page_revisions_changed_by_id",
                table: "wiki_page_revisions",
                column: "changed_by_id");

            migrationBuilder.CreateIndex(
                name: "IX_wiki_page_revisions_page_id_version",
                table: "wiki_page_revisions",
                columns: new[] { "page_id", "version" });

            migrationBuilder.CreateIndex(
                name: "IX_wiki_pages_slug_scope_type_scope_id",
                table: "wiki_pages",
                columns: new[] { "slug", "scope_type", "scope_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "app_config");

            migrationBuilder.DropTable(
                name: "audit_log");

            migrationBuilder.DropTable(
                name: "notes");

            migrationBuilder.DropTable(
                name: "project_members");

            migrationBuilder.DropTable(
                name: "project_sources");

            migrationBuilder.DropTable(
                name: "skill_departments");

            migrationBuilder.DropTable(
                name: "skill_versions");

            migrationBuilder.DropTable(
                name: "source_departments");

            migrationBuilder.DropTable(
                name: "wiki_links");

            migrationBuilder.DropTable(
                name: "wiki_page_drafts");

            migrationBuilder.DropTable(
                name: "wiki_page_revisions");

            migrationBuilder.DropTable(
                name: "projects");

            migrationBuilder.DropTable(
                name: "skills");

            migrationBuilder.DropTable(
                name: "sources");

            migrationBuilder.DropTable(
                name: "wiki_pages");

            migrationBuilder.DropTable(
                name: "employees");

            migrationBuilder.DropTable(
                name: "knowledge_types");

            migrationBuilder.DropTable(
                name: "departments");

            migrationBuilder.DropTable(
                name: "roles");
        }
    }
}
