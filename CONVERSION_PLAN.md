# Kế hoạch Convert Arkon sang .NET + Vue 3

## Stack mới

| Layer | Công nghệ cũ (Python) | Công nghệ mới (.NET/Vue) |
|-------|----------------------|--------------------------|
| Backend Framework | FastAPI | ASP.NET Core 8 Web API |
| ORM | SQLAlchemy async | Entity Framework Core 8 |
| Database (relational) | PostgreSQL | SQLite (dev) / SQL Server (prod) |
| Vector DB | pgvector (PostgreSQL) | ChromaDB (HTTP mode) |
| File Storage | MinIO | Local disk hoặc MinIO .NET SDK |
| Job Queue | Redis + arq | Hangfire + SQLite/SQL Server |
| Auth | JWT (python-jose) + bcrypt | ASP.NET JWT Bearer + BCrypt.Net |
| AI SDK | google-generativeai, openai, anthropic | Google.Generative.AI / Azure.AI.OpenAI / Anthropic.SDK |
| MCP Server | FastMCP | Custom HTTP endpoint (manual) |
| Frontend | Next.js + TypeScript | Vue 3 + Vite + TypeScript |
| State Management | — | Pinia |
| CSS | Tailwind CSS | Tailwind CSS |
| HTTP Client (FE) | fetch | Axios |
| Markdown Render | — | markdown-it hoặc vue-markdown-it |

---

## Cấu trúc thư mục dự án mới

```
ArkonDotNet/
├── backend/                         # ASP.NET Core Web API
│   ├── ArkonApi.sln
│   ├── ArkonApi/
│   │   ├── ArkonApi.csproj
│   │   ├── Program.cs               # Entry point, DI, middleware
│   │   ├── appsettings.json         # Config (DB, JWT, MinIO, Chroma...)
│   │   ├── appsettings.Development.json
│   │   ├── Data/
│   │   │   ├── AppDbContext.cs      # EF Core DbContext
│   │   │   └── Migrations/          # EF migrations
│   │   ├── Models/                  # Entity classes (DB tables)
│   │   │   ├── Source.cs
│   │   │   ├── WikiPage.cs
│   │   │   ├── WikiLink.cs
│   │   │   ├── WikiPageDraft.cs
│   │   │   ├── WikiPageRevision.cs
│   │   │   ├── Employee.cs
│   │   │   ├── Department.cs
│   │   │   ├── Role.cs
│   │   │   ├── Project.cs
│   │   │   ├── ProjectMember.cs
│   │   │   ├── ProjectSource.cs
│   │   │   ├── KnowledgeType.cs
│   │   │   ├── Skill.cs
│   │   │   ├── SkillVersion.cs
│   │   │   ├── AuditLog.cs
│   │   │   ├── AppConfig.cs
│   │   │   └── Enums.cs
│   │   ├── DTOs/                    # Request/Response models
│   │   │   ├── Auth/
│   │   │   ├── Sources/
│   │   │   ├── Wiki/
│   │   │   ├── Projects/
│   │   │   ├── Employees/
│   │   │   └── ...
│   │   ├── Controllers/             # API endpoints
│   │   │   ├── AuthController.cs
│   │   │   ├── SourcesController.cs
│   │   │   ├── WikiController.cs
│   │   │   ├── WikiDraftsController.cs
│   │   │   ├── ProjectsController.cs
│   │   │   ├── SkillsController.cs
│   │   │   ├── RbacController.cs
│   │   │   ├── RolesController.cs
│   │   │   ├── KnowledgeTypesController.cs
│   │   │   ├── AdminSettingsController.cs
│   │   │   ├── AuditController.cs
│   │   │   └── McpController.cs
│   │   ├── Services/                # Business logic
│   │   │   ├── AuthService.cs
│   │   │   ├── PermissionEngine.cs
│   │   │   ├── WikiService.cs
│   │   │   ├── SourceService.cs
│   │   │   ├── StorageService.cs
│   │   │   ├── ChromaService.cs     # Thay pgvector
│   │   │   ├── ConfigService.cs
│   │   │   ├── AuditService.cs
│   │   │   ├── McpAuthService.cs
│   │   │   └── KbService.cs        # Text extraction
│   │   ├── AI/
│   │   │   ├── ILlmProvider.cs      # Interface
│   │   │   ├── IEmbeddingProvider.cs
│   │   │   ├── Providers/
│   │   │   │   ├── OpenAiProvider.cs
│   │   │   │   ├── AnthropicProvider.cs
│   │   │   │   └── GoogleProvider.cs
│   │   │   ├── ProviderRegistry.cs
│   │   │   ├── WikiAgent.cs         # Agent loop
│   │   │   └── WikiAnalyzer.cs
│   │   ├── Jobs/                    # Hangfire background jobs
│   │   │   ├── IngestFileJob.cs
│   │   │   └── CompileWikiJob.cs
│   │   └── Middleware/
│   │       ├── McpAuthMiddleware.cs
│   │       └── ErrorHandlingMiddleware.cs
│
├── frontend/                        # Vue 3 + Vite
│   ├── package.json
│   ├── vite.config.ts
│   ├── tsconfig.json
│   ├── tailwind.config.js
│   ├── index.html
│   └── src/
│       ├── main.ts
│       ├── App.vue
│       ├── router/
│       │   └── index.ts             # Vue Router
│       ├── stores/                  # Pinia stores
│       │   ├── auth.ts
│       │   ├── wiki.ts
│       │   ├── sources.ts
│       │   └── ...
│       ├── api/                     # Axios API calls
│       │   ├── axios.ts             # Base instance + interceptors
│       │   ├── auth.ts
│       │   ├── wiki.ts
│       │   ├── sources.ts
│       │   └── ...
│       ├── views/                   # Pages (= Next.js routes)
│       │   ├── LoginView.vue
│       │   ├── KnowledgeView.vue
│       │   ├── ProjectsView.vue
│       │   ├── WikiDraftsView.vue
│       │   ├── SkillsView.vue
│       │   ├── AuditView.vue
│       │   ├── EmployeesView.vue
│       │   ├── DepartmentsView.vue
│       │   ├── RolesView.vue
│       │   └── SettingsView.vue
│       ├── components/              # Reusable components
│       │   ├── layout/
│       │   ├── wiki/
│       │   ├── shared/
│       │   └── ui/
│       └── types/                   # TypeScript interfaces
│           └── index.ts
```

---

## Giai đoạn 1 — Backend Core (tuần 1-2)

### 1.1 Khởi tạo dự án

```bash
dotnet new sln -n ArkonApi
dotnet new webapi -n ArkonApi --use-controllers
dotnet sln add ArkonApi/ArkonApi.csproj
```

**NuGet packages cần cài:**
```xml
<!-- EF Core -->
<PackageReference Include="Microsoft.EntityFrameworkCore.Sqlite" Version="8.*" />
<PackageReference Include="Microsoft.EntityFrameworkCore.Tools" Version="8.*" />
<PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="8.*" />

<!-- Auth -->
<PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="8.*" />
<PackageReference Include="BCrypt.Net-Next" Version="4.*" />

<!-- File processing -->
<PackageReference Include="PdfPig" Version="0.1.*" />          <!-- PDF extraction -->
<PackageReference Include="DocumentFormat.OpenXml" Version="3.*" /> <!-- Word docs -->
<PackageReference Include="HtmlAgilityPack" Version="1.*" />   <!-- HTML parsing -->

<!-- AI SDKs -->
<PackageReference Include="Azure.AI.OpenAI" Version="2.*" />
<PackageReference Include="Anthropic.SDK" Version="3.*" />

<!-- Background jobs -->
<PackageReference Include="Hangfire.AspNetCore" Version="1.*" />
<PackageReference Include="Hangfire.SQLite" Version="1.*" />   <!-- hoặc SqlServer -->

<!-- HTTP Client (gọi ChromaDB, Google AI) -->
<PackageReference Include="Refit" Version="7.*" />             <!-- strongly-typed HTTP client -->

<!-- Validation -->
<PackageReference Include="FluentValidation.AspNetCore" Version="11.*" />

<!-- Serialization -->
<PackageReference Include="System.Text.Json" />                <!-- built-in -->
```

### 1.2 Database Models (Entity Classes)

Map trực tiếp từ SQLAlchemy models:

**Models/Enums.cs**
```csharp
public enum ScopeType { Global, Project }
public enum WorkspaceRole { Viewer, Contributor, Editor, Admin }
public enum SkillStatus { Active, Processing, Deleting, Deprecated, Archived }
public enum SourceType { File, Url }
```

**Models/Employee.cs**
```csharp
public class Employee
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public string? PasswordHash { get; set; }
    public string Role { get; set; } = "employee"; // "admin" | "employee"
    public Guid DepartmentId { get; set; }
    public Guid? CustomRoleId { get; set; }
    public string? McpToken { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? LastConnected { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public Department Department { get; set; } = null!;
    public Role? CustomRole { get; set; }
}
```

**Models/WikiPage.cs**
```csharp
public class WikiPage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Slug { get; set; } = "";
    public string Title { get; set; } = "";
    public string PageType { get; set; } = "";
    public string ContentMd { get; set; } = "";
    public string Summary { get; set; } = "";
    public string ScopeType { get; set; } = "global";
    public Guid? ScopeId { get; set; }
    // SQLite không hỗ trợ ARRAY — dùng JSON string
    public string KnowledgeTypeSlugsJson { get; set; } = "[]";
    public string SourceIdsJson { get; set; } = "[]";
    // Embedding lưu trong ChromaDB, không lưu ở đây
    public int Version { get; set; } = 1;
    public bool Orphaned { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Computed (không map DB)
    [NotMapped]
    public List<string> KnowledgeTypeSlugs
    {
        get => JsonSerializer.Deserialize<List<string>>(KnowledgeTypeSlugsJson) ?? [];
        set => KnowledgeTypeSlugsJson = JsonSerializer.Serialize(value);
    }
}
```

**Lưu ý quan trọng khi map sang SQLite:**
- `ARRAY(String)` → `TEXT` lưu JSON (serialize/deserialize thủ công)
- `JSONB` → `TEXT` lưu JSON
- `UUID(as_uuid=True)` → `TEXT` (EF Core tự xử lý Guid → TEXT cho SQLite)
- `Vector(768)` → **KHÔNG lưu** — chuyển sang ChromaDB
- `ENUM` của PostgreSQL → `string` + check constraint hoặc C# enum

### 1.3 AppDbContext

```csharp
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
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<AppConfig> AppConfigs => Set<AppConfig>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Composite PK
        modelBuilder.Entity<SourceDepartment>()
            .HasKey(x => new { x.SourceId, x.DepartmentId });
        modelBuilder.Entity<WikiLink>()
            .HasKey(x => new { x.FromSlug, x.ToSlug });
        modelBuilder.Entity<ProjectMember>()
            .HasKey(x => new { x.ProjectId, x.EmployeeId });
        modelBuilder.Entity<ProjectSource>()
            .HasKey(x => new { x.ProjectId, x.SourceId });

        // Unique indexes
        modelBuilder.Entity<Employee>()
            .HasIndex(x => x.Email).IsUnique();
        modelBuilder.Entity<Employee>()
            .HasIndex(x => x.McpToken).IsUnique();
        modelBuilder.Entity<WikiPage>()
            .HasIndex(x => new { x.Slug, x.ScopeType, x.ScopeId }).IsUnique();
    }
}
```

---

## Giai đoạn 2 — Auth & Permissions (tuần 2)

### 2.1 JWT Auth

**Services/AuthService.cs** — thay thế `app/services/auth_service.py`
```csharp
public class AuthService
{
    // Login: verify email/password → tạo JWT
    public async Task<string?> LoginAsync(string email, string password)
    
    // Verify JWT → trả Employee
    public Employee? ValidateToken(string token)
    
    // Hash password (BCrypt)
    public string HashPassword(string password)
    public bool VerifyPassword(string password, string hash)
    
    // Tạo MCP token: "ark_" + random 32 chars
    public string GenerateMcpToken()
}
```

**Controllers/AuthController.cs** — map từ `app/routers/auth.py`
```
POST /api/auth/login          → LoginAsync()
GET  /api/auth/me             → trả current user từ JWT
PUT  /api/auth/change-password → đổi mật khẩu
```

### 2.2 Permission Engine

**Services/PermissionEngine.cs** — thay thế `app/services/permission_engine.py`

Logic chính:
```csharp
public class PermissionEngine
{
    // Kiểm tra employee có permission string không
    // e.g. "doc:read:all", "wiki:edit:own_dept"
    public bool HasPermission(Employee employee, string permission)
    
    // Lấy danh sách department IDs mà employee có thể đọc sources
    public List<Guid> GetAccessibleDepartmentIds(Employee employee)
    
    // Kiểm tra workspace membership
    public WorkspaceRole? GetWorkspaceRole(Guid employeeId, Guid projectId)
    
    // Admin bypass: employee.Role == "admin" → true cho tất cả
}
```

---

## Giai đoạn 3 — File Ingestion & Storage (tuần 2-3)

### 3.1 Storage Service

**Services/StorageService.cs** — thay thế MinIO bằng local disk hoặc MinIO .NET SDK

```csharp
public interface IStorageService
{
    Task<string> UploadAsync(Stream stream, string fileName, string contentType);
    Task<Stream> DownloadAsync(string key);
    Task DeleteAsync(string key);
    Task<string> GetPresignedUrlAsync(string key, int expiryHours = 24);
}

// Implementation 1: Local disk (dev đơn giản)
public class LocalStorageService : IStorageService { ... }

// Implementation 2: MinIO (dùng Minio .NET SDK)
public class MinioStorageService : IStorageService { ... }
```

### 3.2 Text Extraction (KbService)

**Services/KbService.cs** — thay thế `app/services/kb_service.py`

```csharp
public class KbService
{
    // PDF → text (dùng PdfPig)
    public string ExtractPdf(Stream stream)
    
    // Word .docx → text (dùng DocumentFormat.OpenXml)
    public string ExtractDocx(Stream stream)
    
    // HTML → text (dùng HtmlAgilityPack)
    public string ExtractHtml(string html)
    
    // URL → fetch HTML → ExtractHtml
    public Task<string> ExtractUrlAsync(string url)
}
```

### 3.3 Background Jobs (Hangfire)

**Jobs/IngestFileJob.cs** — thay thế arq worker `ingest_file_task`
```csharp
public class IngestFileJob
{
    public async Task ExecuteAsync(Guid sourceId)
    {
        // 1. Download file từ storage
        // 2. Extract text (KbService)
        // 3. Update Source: status=processing, full_text=...
        // 4. Enqueue CompileWikiJob
    }
}
```

**Jobs/CompileWikiJob.cs** — thay thế `compile_wiki_task`
```csharp
public class CompileWikiJob
{
    public async Task ExecuteAsync(Guid sourceId)
    {
        // 1. Gọi WikiAnalyzer → structural map
        // 2. Chạy WikiAgent loop (tối đa 50 steps)
        // 3. Cập nhật embeddings trong ChromaDB
        // 4. Refresh wiki links
        // 5. Update Source: status=ready
        // 6. Ghi AuditLog
    }
}
```

**Đăng ký Hangfire trong Program.cs:**
```csharp
builder.Services.AddHangfire(config =>
    config.UseSQLiteStorage("Data Source=hangfire.db"));
builder.Services.AddHangfireServer(options =>
    options.WorkerCount = 3); // = WORKER_MAX_JOBS

// Enqueue job:
BackgroundJob.Enqueue<IngestFileJob>(j => j.ExecuteAsync(sourceId));
```

---

## Giai đoạn 4 — Vector Search với ChromaDB (tuần 3)

### 4.1 Chạy ChromaDB

```bash
# Cài ChromaDB Python server (hoặc dùng Docker)
pip install chromadb
chroma run --host 0.0.0.0 --port 8001

# Hoặc Docker:
docker run -p 8001:8000 chromadb/chroma
```

### 4.2 ChromaService (.NET)

**Services/ChromaService.cs** — gọi ChromaDB REST API

```csharp
public class ChromaService
{
    private readonly HttpClient _http;
    // ChromaDB REST API base: http://localhost:8001/api/v1
    
    // Upsert embedding cho 1 wiki page
    public async Task UpsertAsync(
        string collection,      // "wiki_pages_global" hoặc "wiki_pages_<projectId>"
        string documentId,      // WikiPage.Slug
        float[] embedding,      // 768-dim vector từ AI provider
        Dictionary<string, string> metadata // scope_type, scope_id, knowledge_type...
    )
    
    // Semantic search: query vector → top K slugs
    public async Task<List<string>> QueryAsync(
        string collection,
        float[] queryEmbedding,
        int topK = 10,
        Dictionary<string, string>? whereFilter = null
    )
    
    // Xóa khi wiki page bị xóa
    public async Task DeleteAsync(string collection, string documentId)
    
    // Tạo collection nếu chưa có
    public async Task EnsureCollectionAsync(string collection)
}
```

**Flow embedding:**
```
WikiPage được tạo/cập nhật
  → Lấy embedding provider từ ProviderRegistry
  → float[] embedding = await provider.EmbedAsync(page.Summary + "\n" + page.ContentMd)
  → chromaService.UpsertAsync("wiki_pages_global", page.Slug, embedding, metadata)

Khi search:
  → float[] queryVec = await provider.EmbedAsync(queryText)
  → List<string> slugs = await chromaService.QueryAsync("wiki_pages_global", queryVec, topK: 10)
  → Load WikiPages từ SQLite bằng slugs → filter quyền → trả về
```

---

## Giai đoạn 5 — AI Integration (tuần 3-4)

### 5.1 Provider Interface

```csharp
public interface ILlmProvider
{
    Task<string> CompleteAsync(string systemPrompt, string userMessage);
    Task<string> CompleteWithToolsAsync(
        string systemPrompt,
        List<ChatMessage> messages,
        List<ToolDefinition> tools,
        CancellationToken ct = default
    );
}

public interface IEmbeddingProvider
{
    Task<float[]> EmbedAsync(string text);
    Task<List<float[]>> EmbedBatchAsync(List<string> texts);
}
```

### 5.2 Provider Implementations

**AI/Providers/OpenAiProvider.cs:**
```csharp
// Dùng Azure.AI.OpenAI hoặc OpenAI SDK
public class OpenAiProvider : ILlmProvider, IEmbeddingProvider
{
    // LLM: OpenAI chat completions với tool calling
    // Embedding: text-embedding-3-small → float[1536] → project xuống 768
}
```

**AI/Providers/AnthropicProvider.cs:**
```csharp
// Dùng Anthropic.SDK
public class AnthropicProvider : ILlmProvider
{
    // Claude 3.5 Sonnet/Haiku với tool use
    // Không có embedding → delegate sang OpenAI/Google
}
```

**AI/Providers/GoogleProvider.cs:**
```csharp
// Gọi REST API Gemini trực tiếp (hoặc Google.Generative.AI SDK)
public class GoogleProvider : ILlmProvider, IEmbeddingProvider
{
    // Embedding: text-embedding-004 → float[768] (mặc định)
    // LLM: gemini-1.5-flash / gemini-2.0-flash
}
```

### 5.3 ProviderRegistry

**AI/ProviderRegistry.cs** — thay `app/ai/registry.py`
```csharp
public class ProviderRegistry
{
    // Đọc config từ DB (AppConfig table) → khởi tạo provider đúng
    public async Task<ILlmProvider> GetLlmAsync()
    public async Task<IEmbeddingProvider> GetEmbeddingAsync()
}
```

### 5.4 WikiAnalyzer

**AI/WikiAnalyzer.cs** — thay `app/ai/wiki_analyzer.py`

```csharp
public class WikiAnalyzer
{
    // Gọi LLM với source text → trả về structural map JSON:
    // {
    //   "document_type": "SOP",
    //   "pages_to_create": [{"slug": "xxx", "title": "..."}],
    //   "pages_to_update": ["existing-slug"],
    //   "entities": ["CompanyA", "Process B"]
    // }
    public async Task<WikiAnalysisResult> AnalyzeAsync(string sourceText, string existingWikiIndex)
}
```

### 5.5 WikiAgent (Tool-Calling Loop)

**AI/WikiAgent.cs** — thay `app/ai/wiki_agent.py`

```csharp
public class WikiAgent
{
    private const int MaxSteps = 50;
    
    // Tools giống Python: read_wiki_index, read_wiki_page, search_wiki,
    //   read_source_excerpt, create_page, update_page, append_log, finish
    
    public async Task RunAsync(Guid sourceId, CancellationToken ct)
    {
        var messages = new List<ChatMessage>();
        messages.Add(SystemMessage(BuildSystemPrompt()));
        messages.Add(UserMessage(await BuildInitialMessage(sourceId)));
        
        for (int step = 0; step < MaxSteps; step++)
        {
            var response = await _llm.CompleteWithToolsAsync(
                systemPrompt: "", messages, tools: GetToolDefinitions(), ct);
            
            if (response.IsFinish) break;
            
            // Dispatch tool call
            var toolResult = await DispatchToolAsync(response.ToolCall, sourceId);
            messages.Add(AssistantMessage(response));
            messages.Add(ToolResultMessage(toolResult));
        }
    }
}
```

**Tool implementations trong WikiAgent:**
- `read_wiki_index` → query SQLite WikiPages (title, slug, summary)
- `read_wiki_page` → query WikiPage by slug
- `search_wiki` → ChromaDB query → load pages
- `read_source_excerpt` → slice source.FullText by char offset
- `create_page` → INSERT WikiPage + ChromaDB upsert + WikiPageRevision
- `update_page` → UPDATE WikiPage + ChromaDB upsert + WikiPageRevision
- `finish` → set done flag

---

## Giai đoạn 6 — API Controllers (tuần 4)

### Map 1-1 từ Python routers

**Controllers/SourcesController.cs**
```
POST   /api/sources/upload          → Upload file → IngestFileJob.Enqueue
GET    /api/sources                 → List sources (filter by quyền)
GET    /api/sources/{id}            → Get source detail + progress
DELETE /api/sources/{id}            → Xóa source + file storage
POST   /api/sources/{id}/recompile  → Re-enqueue CompileWikiJob
GET    /api/sources/{id}/progress   → SSE hoặc polling status
```

**Controllers/WikiController.cs**
```
GET    /api/wiki/pages              → List pages (filter scope + quyền)
GET    /api/wiki/pages/{slug}       → Get page content
POST   /api/wiki/pages              → Create page (editor only)
PUT    /api/wiki/pages/{slug}       → Update page (editor only)
DELETE /api/wiki/pages/{slug}       → Delete page (admin only)
GET    /api/wiki/search             → Full-text + semantic search
GET    /api/wiki/pages/{slug}/links → Get backlinks + outlinks
GET    /api/wiki/pages/{slug}/revisions → Get version history
```

**Controllers/WikiDraftsController.cs**
```
POST   /api/wiki/pages/{slug}/drafts → Propose draft (contributor)
GET    /api/wiki/drafts              → List pending drafts (editor)
PUT    /api/wiki/drafts/{id}/approve → Approve draft
PUT    /api/wiki/drafts/{id}/reject  → Reject with note
```

**Controllers/ProjectsController.cs**
```
GET    /api/projects               → List projects (member sees own, admin sees all)
POST   /api/projects               → Create project
PUT    /api/projects/{id}          → Update
DELETE /api/projects/{id}          → Delete
GET    /api/projects/{id}/members  → List members
POST   /api/projects/{id}/members  → Add member
PUT    /api/projects/{id}/members/{empId} → Change role
DELETE /api/projects/{id}/members/{empId} → Remove member
GET    /api/projects/{id}/sources  → List project sources
POST   /api/projects/{id}/sources  → Add source to project
```

**Controllers/RbacController.cs**
```
GET    /api/departments            → List
POST   /api/departments            → Create
PUT    /api/departments/{id}       → Update
DELETE /api/departments/{id}       → Delete
GET    /api/employees              → List
POST   /api/employees              → Create
PUT    /api/employees/{id}         → Update (role, dept, custom_role)
DELETE /api/employees/{id}         → Delete
POST   /api/employees/{id}/mcp-token → Generate/regenerate MCP token
```

---

## Giai đoạn 7 — MCP Server (tuần 5, tùy chọn)

MCP là protocol đơn giản: HTTP POST với JSON-RPC. Không cần framework riêng.

**Controllers/McpController.cs**
```csharp
[Route("/mcp")]
public class McpController : ControllerBase
{
    // Authenticate bằng Bearer token (McpAuthService)
    // Dispatch tools:
    
    // list_tools → trả danh sách tools available
    // call_tool  → dispatch theo tool name:
    //   search_wiki(query, top_k)
    //   read_wiki_page(slug)
    //   list_wiki_pages(knowledge_type?, scope?)
    //   get_source_pages(source_id, page_from, page_to)
    //   list_skills()
    //   propose_wiki_edit(slug, content, note)
}
```

---

## Giai đoạn 8 — Frontend Vue 3 (tuần 5-6)

### 8.1 Khởi tạo

```bash
npm create vite@latest frontend -- --template vue-ts
cd frontend
npm install
npm install vue-router@4 pinia axios
npm install -D tailwindcss @tailwindcss/typography autoprefixer
npm install markdown-it @types/markdown-it
npm install lucide-vue-next          # icons
```

### 8.2 Cấu trúc Router

**src/router/index.ts**
```typescript
const routes = [
  { path: '/login', component: LoginView },
  {
    path: '/',
    component: PortalLayout,          // sidebar + header
    meta: { requiresAuth: true },
    children: [
      { path: '', redirect: '/knowledge' },
      { path: 'knowledge', component: KnowledgeView },
      { path: 'projects', component: ProjectsView },
      { path: 'wiki/drafts', component: WikiDraftsView },
      { path: 'skills', component: SkillsView },
      { path: 'employees', component: EmployeesView },
      { path: 'departments', component: DepartmentsView },
      { path: 'roles', component: RolesView },
      { path: 'audit', component: AuditView },
      { path: 'settings', component: SettingsView },
      { path: 'profile', component: ProfileView },
    ]
  }
]
```

### 8.3 Pinia Stores

**src/stores/auth.ts**
```typescript
export const useAuthStore = defineStore('auth', {
  state: () => ({
    token: localStorage.getItem('token') ?? null,
    employee: null as Employee | null,
  }),
  actions: {
    async login(email: string, password: string) {
      const { data } = await authApi.login(email, password)
      this.token = data.access_token
      localStorage.setItem('token', data.access_token)
      await this.fetchMe()
    },
    async fetchMe() {
      const { data } = await authApi.me()
      this.employee = data
    },
    logout() {
      this.token = null
      this.employee = null
      localStorage.removeItem('token')
    }
  }
})
```

### 8.4 Axios Setup

**src/api/axios.ts**
```typescript
const api = axios.create({
  baseURL: import.meta.env.VITE_API_URL || 'http://localhost:5000',
})

// Request interceptor: tự động đính kèm JWT
api.interceptors.request.use(config => {
  const token = localStorage.getItem('token')
  if (token) config.headers.Authorization = `Bearer ${token}`
  return config
})

// Response interceptor: 401 → redirect login
api.interceptors.response.use(
  res => res,
  err => {
    if (err.response?.status === 401) router.push('/login')
    return Promise.reject(err)
  }
)

export default api
```

### 8.5 Wiki Browser Component (3 panels)

**src/views/KnowledgeView.vue**
```vue
<template>
  <div class="flex h-full">
    <!-- Panel 1: Page tree (width: 240px) -->
    <WikiTree @select="selectPage" />
    
    <!-- Panel 2: Page content (flex-1) -->
    <WikiPageViewer :slug="selectedSlug" />
    
    <!-- Panel 3: Links graph (width: 280px) -->
    <WikiLinksPanel :slug="selectedSlug" />
  </div>
</template>
```

### 8.6 Design System (Sahara — giữ nguyên)

**tailwind.config.js**
```javascript
module.exports = {
  theme: {
    extend: {
      colors: {
        primary: '#c2652a',   // burnt sienna
        surface: '#faf5ee',   // warm linen
        accent: '#8c3c3c',    // dusty rose
      },
      fontFamily: {
        serif: ['EB Garamond', 'Georgia', 'serif'],
        sans: ['Manrope', 'system-ui', 'sans-serif'],
      }
    }
  }
}
```

---

## Giai đoạn 9 — Startup & Seed Data (tuần 6)

**Program.cs** — tương đương `app/main.py`

```csharp
var builder = WebApplication.CreateBuilder(args);

// --- Services ---
builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseSqlite("Data Source=arkon.db"));

builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<PermissionEngine>();
builder.Services.AddScoped<WikiService>();
builder.Services.AddScoped<SourceService>();
builder.Services.AddScoped<ChromaService>();
builder.Services.AddScoped<AuditService>();
builder.Services.AddScoped<KbService>();
builder.Services.AddScoped<ProviderRegistry>();
builder.Services.AddScoped<WikiAgent>();
builder.Services.AddScoped<WikiAnalyzer>();
builder.Services.AddSingleton<IStorageService, LocalStorageService>();

// JWT Auth
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options => { /* config */ });

// Hangfire
builder.Services.AddHangfire(c => c.UseSQLiteStorage("Data Source=hangfire.db"));
builder.Services.AddHangfireServer(o => o.WorkerCount = 3);

// CORS
builder.Services.AddCors(opt => opt.AddDefaultPolicy(p =>
    p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

builder.Services.AddControllers();

var app = builder.Build();

// --- Middleware ---
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.UseHangfireDashboard("/hangfire");
app.MapControllers();

// --- Seed admin on first run ---
await SeedAdminAsync(app);

app.Run();
```

---

## Thứ tự thực hiện tổng hợp

```
Tuần 1: Models + DbContext + EF Migration + Auth (login, JWT)
Tuần 2: Permission Engine + Sources CRUD + File Upload + Storage
Tuần 3: ChromaDB integration + AI Providers + WikiAnalyzer
Tuần 4: WikiAgent loop + CompileWikiJob + Hangfire
Tuần 5: Wiki CRUD + Drafts + Projects + RBAC controllers
Tuần 6: Vue 3 setup + Router + Auth store + Wiki viewer
Tuần 7: Toàn bộ Vue views + Pinia stores + API calls
Tuần 8: MCP endpoint + polish + testing
```

---

## Các điểm cần chú ý khi convert

1. **PostgreSQL ARRAY → JSON string**: Mọi chỗ dùng `ARRAY` trong models cần serialize thành JSON text trong SQLite.

2. **pgvector → ChromaDB**: Embedding không lưu trong bảng WikiPages nữa. Mỗi lần tạo/cập nhật wiki page → gọi ChromaDB upsert. Khi xóa → gọi ChromaDB delete.

3. **async/await**: FastAPI dùng Python async, .NET cũng dùng async/await — pattern tương tự.

4. **Background jobs**: Python dùng Redis + arq worker process riêng. .NET dùng Hangfire — chạy trong cùng process (in-process), đơn giản hơn nhiều.

5. **Tool calling format**: Mỗi AI provider có format tool call khác nhau. Cần normalize lại ở tầng `ILlmProvider`.

6. **AppConfig encryption**: Python dùng Fernet để encrypt API keys trong DB. .NET dùng `DataProtection API` của ASP.NET Core cho mục đích tương tự.

7. **WikiLinks (`[[slug]]`)**: Cần regex parse content markdown sau mỗi lần update để rebuild bảng `wiki_links`.

8. **Full-text search**: Không có pgvector → dùng kết hợp SQLite FTS5 (full-text) + ChromaDB (semantic).
