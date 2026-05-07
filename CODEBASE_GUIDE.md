# Javis — Hướng Dẫn Codebase cho Team

> Tài liệu này giải thích cấu trúc, luồng dữ liệu, và cách mỗi file kết nối với nhau.
> Đọc file này trước khi bắt đầu code bất kỳ tính năng nào.

---

## Mục lục

1. [Kiến trúc tổng quan](#1-kiến-trúc-tổng-quan)
2. [Cấu trúc thư mục](#2-cấu-trúc-thư-mục)
3. [Luồng cốt lõi — Upload tài liệu → Wiki](#3-luồng-cốt-lõi--upload-tài-liệu--wiki)
4. [Luồng xác thực — Login & JWT](#4-luồng-xác-thực--login--jwt)
5. [Luồng MCP — Claude Desktop query](#5-luồng-mcp--claude-desktop-query)
6. [Hệ thống phân quyền RBAC](#6-hệ-thống-phân-quyền-rbac)
7. [Database — Các bảng chính](#7-database--các-bảng-chính)
8. [AI System — Provider, Analyzer, Agent](#8-ai-system--provider-analyzer-agent)
9. [Frontend — Vue 3 routing & state](#9-frontend--vue-3-routing--state)
10. [Config & Environment](#10-config--environment)
11. [Quy ước code](#11-quy-ước-code)
12. [Hướng dẫn thêm tính năng mới](#12-hướng-dẫn-thêm-tính-năng-mới)

---

## 1. Kiến trúc tổng quan

```
┌─────────────────────────────────────────────────────────────────┐
│                         NGƯỜI DÙNG                              │
│  Browser (Vue 3 :5173)          Claude Desktop (MCP token)      │
└──────────────┬──────────────────────────────┬───────────────────┘
               │ HTTP REST /api/*             │ POST /mcp
               ▼                              ▼
┌──────────────────────────────────────────────────────────────────┐
│                    ASP.NET Core 8 (:5000)                        │
│                                                                   │
│  Controllers/         Services/              AI/                  │
│  ├── Auth             ├── AuthService        ├── WikiAnalyzer     │
│  ├── Sources          ├── WikiService        ├── WikiAgent        │
│  ├── Wiki             ├── PermissionEngine   ├── ProviderRegistry │
│  ├── WikiDrafts       ├── ChromaService      └── Providers/       │
│  ├── Projects         ├── ConfigService           ├── Anthropic   │
│  ├── Rbac             ├── KbService               ├── OpenAI      │
│  ├── Admin            ├── McpAuthService          └── Google      │
│  └── McpController    ├── StorageService                          │
│                       └── AuditService                            │
│                                                                   │
│  Jobs/ (Hangfire)                                                 │
│  ├── IngestFileJob    ← chạy nền, extract text                   │
│  └── CompileWikiJob  ← chạy nền, gọi WikiAgent                   │
└──────┬───────────────────────────┬────────────────────────────────┘
       │                           │
       ▼                           ▼
 SQLite (javis.db)          ChromaDB (:8001)
 — tất cả entities          — vector embeddings
 — wiki content             — semantic search
```

**Nguyên tắc:**
- Controller → Service → Database. Controller không query DB trực tiếp.
- Jobs chạy async qua Hangfire, không block HTTP request.
- AI Provider được inject qua `ProviderRegistry` — không hardcode provider cụ thể.

---

## 2. Cấu trúc thư mục

```
Javis/
├── backend/
│   └── JavisApi/
│       ├── Program.cs              ← Điểm khởi động, DI, middleware
│       ├── appsettings.json        ← Config (DB, JWT, ChromaDB, Storage)
│       │
│       ├── Controllers/            ← HTTP endpoints (route handlers)
│       │   ├── AuthController.cs       /api/auth/*
│       │   ├── SourcesController.cs    /api/sources/*
│       │   ├── WikiController.cs       /api/wiki/*
│       │   ├── WikiDraftsController.cs /api/wiki-drafts/*
│       │   ├── ProjectsController.cs   /api/projects/*
│       │   ├── RbacController.cs       /api/departments|employees|roles|knowledge-types/*
│       │   ├── AdminController.cs      /api/admin/*
│       │   └── McpController.cs        /mcp  (JSON-RPC 2.0)
│       │
│       ├── Services/               ← Business logic
│       │   ├── AuthService.cs          Login, JWT, BCrypt, MCP token
│       │   ├── WikiService.cs          CRUD wiki, search, links, draft
│       │   ├── PermissionEngine.cs     RBAC checks (global + workspace)
│       │   ├── ChromaService.cs        ChromaDB HTTP client
│       │   ├── ConfigService.cs        Đọc/ghi AI settings (AES encrypted)
│       │   ├── KbService.cs            Extract text từ PDF/Word/HTML/URL
│       │   ├── StorageService.cs       Upload/download file
│       │   ├── McpAuthService.cs       Xác thực ark_xxx token
│       │   └── AuditService.cs         Ghi audit log
│       │
│       ├── AI/                     ← AI pipeline
│       │   ├── ILlmProvider.cs         Interface + shared types
│       │   ├── ProviderRegistry.cs     Chọn provider từ DB config
│       │   ├── WikiAnalyzer.cs         Phân tích sơ bộ tài liệu
│       │   ├── WikiAgent.cs            Tool-calling agent loop
│       │   └── Providers/
│       │       ├── AnthropicProvider.cs   Claude via Anthropic.SDK
│       │       ├── OpenAiProvider.cs      GPT via Azure.AI.OpenAI
│       │       └── GoogleProvider.cs      Gemini via REST API
│       │
│       ├── Jobs/                   ← Hangfire background jobs
│       │   ├── IngestFileJob.cs        Extract text → enqueue compile
│       │   └── CompileWikiJob.cs       Run WikiAgent
│       │
│       ├── Models/                 ← EF Core entities (database tables)
│       │   ├── Enums.cs                ScopeType, WorkspaceRole, Status enums
│       │   ├── Employee.cs             User (role: admin|employee)
│       │   ├── Department.cs           Phòng ban
│       │   ├── Role.cs                 Custom permission role
│       │   ├── Source.cs               Tài liệu nguồn
│       │   ├── Wiki.cs                 WikiPage, WikiLink, WikiPageDraft, WikiPageRevision
│       │   ├── Project.cs              Project/Workspace + Member + Source
│       │   ├── Skill.cs                Skill + SkillVersion + SkillDepartment
│       │   ├── KnowledgeType.cs        Loại tài liệu (SOP, Policy...)
│       │   └── Misc.cs                 AuditLog, AppConfig, Note
│       │
│       ├── DTOs/                   ← Request/Response shapes (không phải entity)
│       │   ├── Auth/AuthDtos.cs
│       │   ├── Sources/SourceDtos.cs
│       │   ├── Wiki/WikiDtos.cs
│       │   ├── Projects/ProjectDtos.cs
│       │   ├── Employees/EmployeeDtos.cs
│       │   └── Common/CommonDtos.cs     PagedResult<T>, ApiError, AuditLogDto
│       │
│       ├── Data/
│       │   └── AppDbContext.cs      DbSets, relationships, indexes
│       │
│       └── Migrations/             ← EF Core migration files (tự sinh)
│
└── frontend/
    └── src/
        ├── main.ts                 ← Khởi động app, mount Pinia + Router
        ├── App.vue                 ← Root component (chỉ có <RouterView>)
        ├── style.css               ← Tailwind + custom theme (sienna, linen)
        │
        ├── api/                    ← Axios HTTP calls đến backend
        │   ├── axios.ts                Instance với interceptors (JWT header, 401 redirect)
        │   ├── auth.ts                 authApi.login(), authApi.me()
        │   ├── wiki.ts                 wikiApi.list(), wikiApi.get(), wikiApi.search()
        │   └── index.ts                sourcesApi, draftsApi, projectsApi, adminApi, rbacApi
        │
        ├── stores/                 ← Pinia stores (global state)
        │   └── auth.ts                 token, employee, isLoggedIn, isAdmin, login(), logout()
        │
        ├── router/
        │   └── index.ts            ← Route definitions + navigation guards
        │
        ├── components/
        │   └── layout/
        │       ├── AppLayout.vue   ← Sidebar + main content wrapper
        │       ├── NavGroup.vue    ← Sidebar section header
        │       └── NavItem.vue     ← Sidebar menu item
        │
        └── views/
            ├── LoginView.vue
            ├── ProfileView.vue
            ├── SourcesView.vue
            ├── DraftsView.vue
            ├── ProjectsView.vue
            ├── ProjectDetailView.vue
            ├── wiki/
            │   ├── WikiBrowserView.vue  ← Panel trái: danh sách trang + search
            │   └── WikiPageView.vue     ← Panel phải: render markdown
            └── admin/
                ├── EmployeesView.vue
                ├── DepartmentsView.vue
                ├── RolesView.vue
                ├── KnowledgeTypesView.vue
                ├── SettingsView.vue
                └── AuditView.vue
```

---

## 3. Luồng cốt lõi — Upload tài liệu → Wiki

Đây là luồng quan trọng nhất của hệ thống.

```
[Browser] POST /api/sources/upload  (multipart/form-data)
    │
    ▼
[SourcesController.UploadFile()]
    ├── Kiểm tra quyền: PermissionEngine.CanUploadSource()
    ├── Lưu file: IStorageService.UploadAsync()  → uploads/{guid}_{filename}
    ├── Tạo Source record trong DB (status = "pending")
    └── Enqueue job: IBackgroundJobClient.Enqueue<IngestFileJob>(sourceId)
    │
    ▼  (HTTP trả về ngay, user không cần chờ)
    
[IngestFileJob.ExecuteAsync(sourceId)]   ← Hangfire worker thread
    ├── source.Status = "processing", source.Progress = 10%
    ├── Nếu source_type == "file":
    │       IStorageService.DownloadAsync() → Stream
    │       KbService.ExtractFromFileAsync(stream, filename)
    │           ├── .pdf  → PdfPig extract text per page
    │           ├── .docx → DocumentFormat.OpenXml
    │           ├── .html/.htm → HtmlAgilityPack (strip tags)
    │           └── khác  → đọc như text
    ├── Nếu source_type == "url":
    │       KbService.ExtractUrlAsync(url)
    │           → HttpClient GET → HtmlAgilityPack → extract body text
    ├── source.FullText = extractedText, source.Progress = 20%
    └── Enqueue: IBackgroundJobClient.Enqueue<CompileWikiJob>(sourceId)
    │
    ▼
[CompileWikiJob.ExecuteAsync(sourceId)]   ← Hangfire worker thread
    ├── WikiAnalyzer.AnalyzeAsync(source.FullText, wikiIndex)
    │       → 1 LLM call (cheap model)
    │       → trả về: { pages_to_create: [...], pages_to_update: [...] }
    └── WikiAgent.RunAsync(sourceId)
    │
    ▼
[WikiAgent] — vòng lặp tối đa 50 bước
    │
    ├── BƯỚC 1: Gọi LLM với system prompt + initial message
    │       System prompt: "Bạn là wiki editor. Tạo/cập nhật trang wiki từ tài liệu nguồn.
    │                       Dùng Markdown, đặt [[wikilinks]], tối thiểu 80 từ/trang."
    │       Initial message: tóm tắt source + wikiIndex hiện tại + phân tích từ WikiAnalyzer
    │
    ├── BƯỚC N: LLM gọi một trong 8 tools:
    │
    │   read_wiki_index        → WikiService.BuildWikiIndexAsync()
    │                            → danh sách slug + title + summary hiện có
    │
    │   read_wiki_page(slug)   → WikiService.GetBySlugAsync(slug, scopeType, scopeId)
    │                            → nội dung Markdown của trang
    │
    │   search_wiki(query)     → WikiService.FullTextSearchAsync()
    │                            → top 5 trang phù hợp
    │
    │   read_source_excerpt    → source.FullText[charStart..charEnd]
    │       (charStart, charEnd)  → đọc đoạn cụ thể từ tài liệu gốc
    │
    │   create_page(slug,      → Kiểm tra >= 80 words
    │     title, content_md,   → WikiService.CreatePageAsync()
    │     summary)             → EmbeddingProvider.EmbedAsync(title + summary)
    │                          → ChromaService.UpsertAsync(embedding)
    │                          → WikiService.RefreshLinksAsync(slug) ← parse [[links]]
    │
    │   update_page(slug,      → WikiService.UpdatePageAsync()
    │     content_md, summary) → Re-embed → ChromaDB upsert
    │
    │   append_log(entry)      → Thêm vào trang _log trong wiki (activity log)
    │
    │   finish                 → Kết thúc vòng lặp
    │
    └── Sau vòng lặp:
            source.Status = "ready", source.Progress = 100%
            AuditService.LogAsync("wiki_compiled", sourceId)
```

**File liên quan:**
- `Controllers/SourcesController.cs` — điểm vào HTTP
- `Services/KbService.cs` — extract text
- `Services/StorageService.cs` — lưu/tải file
- `Jobs/IngestFileJob.cs` → `Jobs/CompileWikiJob.cs`
- `AI/WikiAnalyzer.cs` — phân tích sơ bộ
- `AI/WikiAgent.cs` — agent loop + tool dispatcher
- `Services/WikiService.cs` — CRUD wiki, search, links
- `Services/ChromaService.cs` — vector DB

---

## 4. Luồng xác thực — Login & JWT

```
[Browser] POST /api/auth/login  { email, password }
    │
    ▼
[AuthController.Login()]
    ├── AuthService.ValidateCredentialsAsync(email, password)
    │       ├── Query DB: Employee WHERE email = ? AND is_active = true
    │       │             .Include(Department).Include(CustomRole)
    │       └── BCrypt.Verify(password, employee.PasswordHash)
    │
    ├── AuthService.GenerateJwtToken(employee)
    │       → Claims: sub=id, email, name, role, dept_id
    │       → HS256, expires 7 ngày (config: Jwt:ExpiryDays)
    │
    └── Trả về: { token: "eyJ...", employee: { id, name, email, role, ... } }
    
[Browser] lưu token vào localStorage
    → axios interceptor tự động thêm: Authorization: Bearer eyJ...
    
[Mọi request tiếp theo]
    → ASP.NET JwtBearer middleware xác thực token
    → Inject HttpContext.User.Claims
    → Controller lấy employee ID từ claims:
          var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!)
          var employee = await _db.Employees.Include(e => e.CustomRole).FindAsync(userId)
```

**MCP Token (Claude Desktop):**
```
[Admin] POST /api/employees/{id}/mcp-token
    → AuthService.GenerateMcpToken()
    → "ark_" + 32 ký tự hex ngẫu nhiên
    → Lưu vào employee.McpToken trong DB

[Claude Desktop] POST /mcp
    Authorization: Bearer ark_xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx
    → McpAuthService.VerifyTokenAsync(token)
    → Query DB: Employee WHERE mcp_token = ?
    → Cập nhật employee.LastConnected = now
```

**File liên quan:**
- `Controllers/AuthController.cs`
- `Services/AuthService.cs`
- `Services/McpAuthService.cs`
- `frontend/src/stores/auth.ts`
- `frontend/src/api/axios.ts` — interceptors

---

## 5. Luồng MCP — Claude Desktop query

```
Claude Desktop (đã cấu hình server URL + ark_xxx token)
    │
    ▼
POST /mcp
{
  "method": "tools/call",
  "params": {
    "name": "search_wiki",
    "arguments": "{\"query\": \"quy trình onboarding\"}"
  }
}
    │
    ▼
[McpController.Handle()]
    ├── Xác thực: McpAuthService.VerifyTokenAsync(Bearer token)
    ├── req.Method == "tools/list" → trả về 3 tools có sẵn
    └── req.Method == "tools/call" → HandleToolCall()
            ├── "search_wiki" → WikiService.FullTextSearchAsync()
            │       → PermissionEngine.FilterWikiPagesAsync()  ← RBAC
            │       → Trả về list { slug, title, excerpt }
            │
            ├── "read_wiki_page" → WikiService.GetBySlugAsync()
            │       → Trả về toàn bộ nội dung Markdown
            │
            └── "list_wiki_pages" → WikiService.BuildWikiIndexAsync()
                    → Trả về index tất cả trang

3 tools available cho Claude:
┌─────────────────┬─────────────────────────────────────────────────┐
│ Tool            │ Mô tả                                           │
├─────────────────┼─────────────────────────────────────────────────┤
│ search_wiki     │ Tìm kiếm full-text, trả về top kết quả         │
│ read_wiki_page  │ Đọc toàn bộ nội dung 1 trang theo slug         │
│ list_wiki_pages │ Lấy danh sách tất cả trang (slug + title)      │
└─────────────────┴─────────────────────────────────────────────────┘
```

**File liên quan:**
- `Controllers/McpController.cs`
- `Services/McpAuthService.cs`
- `Services/WikiService.cs`
- `Services/PermissionEngine.cs`

---

## 6. Hệ thống phân quyền RBAC

Javis dùng **dual-realm**: Global + Workspace.

### Realm 1: Global (toàn tổ chức)

```
Employee
├── role: "admin"   → bypass TẤT CẢ checks (IsAdmin() = true)
└── role: "employee"
    └── custom_role_id → CustomRole
                          └── Permissions (JSON array):
                              "doc:read:all"        → đọc mọi tài liệu
                              "doc:read:own_dept"   → chỉ tài liệu phòng ban mình
                              "doc:upload"          → upload tài liệu
                              "doc:delete"          → xóa tài liệu
                              "wiki:read:all"       → đọc mọi wiki
                              "wiki:read:own_dept"  → chỉ wiki phòng ban mình
                              "wiki:edit:all"       → sửa wiki
                              "org:settings:manage" → cấu hình AI
                              "*"                   → wildcard = tất cả
```

**Cách dùng trong Controller:**
```csharp
// Lấy employee từ JWT claims
var employee = await GetCurrentEmployeeAsync(); // helper method

// Kiểm tra quyền
if (!_permissions.CanUploadSource(employee))
    return Forbid();

// Filter danh sách theo quyền
var sources = await _permissions.FilterSources(employee, _db.Sources).ToListAsync();
```

### Realm 2: Workspace (Project)

```
Project
└── ProjectMembers
    ├── employeeId + projectId
    └── role: viewer | contributor | editor | admin

PermissionEngine checks:
  CanViewProjectAsync()       → role >= viewer
  CanContributeToProjectAsync() → role >= contributor (đề xuất draft)
  CanEditProjectAsync()       → role >= editor (approve draft, sửa wiki)
  CanAdminProjectAsync()      → role == admin (thêm/xóa member)
```

### Department Scoping (nguồn tài liệu)

```
Source có SourceDepartments (bảng M2M):
  - Không có row nào  → tài liệu GLOBAL (ai cũng xem được nếu có quyền read)
  - Có rows           → chỉ employee thuộc department đó mới xem được

FilterSources() trong PermissionEngine:
  if admin → return all
  if has doc:read:all → return all (không có dept restriction)  
  if has doc:read:own_dept → return only sources có dept = employee.DepartmentId
                              hoặc sources không có dept nào (global)
```

**File liên quan:**
- `Services/PermissionEngine.cs` — toàn bộ logic
- `Models/Role.cs` — CustomRole với Permissions JSON
- `Models/Project.cs` — ProjectMember với WorkspaceRole

---

## 7. Database — Các bảng chính

```
departments          employees              custom_roles
├── id (PK)         ├── id (PK)            ├── id (PK)
├── name            ├── name               ├── name
└── description     ├── email              └── permissions_json   ← JSON array
                    ├── password_hash
                    ├── role               knowledge_types
                    ├── is_active          ├── id (PK)
                    ├── mcp_token          ├── name
                    ├── department_id (FK) └── slug
                    └── custom_role_id(FK)

sources                         wiki_pages
├── id (PK)                    ├── id (PK)
├── title                      ├── slug (UNIQUE per scope)
├── source_type (file|url)     ├── title
├── scope_type (global|project)├── page_type (article|index|log)
├── scope_id                   ├── content_md
├── status (pending→ready)     ├── summary
├── progress (0-100)           ├── scope_type
├── progress_message           ├── scope_id
├── full_text                  ├── knowledge_type_slugs_json ← JSON []
├── minio_key                  ├── source_ids_json           ← JSON []
├── url                        ├── version
└── knowledge_type_id (FK)     └── orphaned

wiki_links              wiki_page_drafts        wiki_page_revisions
├── source_slug (FK)   ├── id (PK)             ├── id (PK)
└── target_slug (FK)   ├── wiki_page_id (FK)   ├── wiki_page_id (FK)
                       ├── proposed_by (FK)    ├── revision_number
                       ├── status (pending...) ├── content_md
                       └── content_md          └── created_at

source_departments    projects               project_members
├── source_id (FK)   ├── id (PK)            ├── project_id (FK)
└── department_id    ├── name               ├── employee_id (FK)
                     ├── scope_type         └── role (viewer..admin)
                     └── description

app_config           audit_logs
├── key (PK)        ├── id (PK)
└── value (encrypted)└── action, actor_id, resource_type, details
```

**Lưu ý quan trọng:**
- `wiki_pages.slug` là unique TRONG CÙNG scope (global hoặc project). Cùng slug có thể tồn tại ở global và project khác nhau.
- Embedding **KHÔNG lưu trong SQLite** — chỉ lưu trong ChromaDB với collection name `wiki_pages_global` hoặc `wiki_pages_project_{id}`.
- `app_config.value` được **encrypt AES-256** bởi `ConfigService` — không đọc trực tiếp từ DB.

**File liên quan:**
- `Data/AppDbContext.cs` — relationships, indexes, OnDelete behavior
- `Models/*.cs` — entity definitions

---

## 8. AI System — Provider, Analyzer, Agent

### ProviderRegistry — chọn provider động

```csharp
// ProviderRegistry đọc từ app_config trong DB:
//   key "llm_provider"       → "anthropic" | "openai" | "google"
//   key "embedding_provider" → "openai" | "google"
//   key "anthropic_api_key"  → (encrypted)
//   key "openai_api_key"     → (encrypted)
//   key "google_api_key"     → (encrypted)

var llm = await _registry.GetLlmAsync();         // → ILlmProvider
var emb = await _registry.GetEmbeddingAsync();   // → IEmbeddingProvider

// Thay đổi provider: Admin cập nhật qua /api/admin/settings
// ProviderRegistry.Invalidate() được gọi → cache xóa → lần sau đọc lại từ DB
```

### Interface — ILlmProvider

```csharp
public interface ILlmProvider
{
    string ProviderName { get; }

    // Dùng cho: WikiAnalyzer, simple completions
    Task<string> CompleteAsync(string systemPrompt, string userMessage, CancellationToken ct);

    // Dùng cho: WikiAgent tool-calling loop
    Task<LlmToolCallResponse> CompleteWithToolsAsync(
        string systemPrompt,
        List<ChatMessage> messages,
        List<ToolDefinition> tools,
        CancellationToken ct);
}

public interface IEmbeddingProvider
{
    // Dùng để embed wiki page title+summary → vector lưu vào ChromaDB
    Task<float[]> EmbedAsync(string text, CancellationToken ct);
}
```

### WikiAnalyzer — phân tích sơ bộ (1 LLM call)

```
Input:  sourceText (2000 ký tự đầu) + wikiIndex hiện tại
Output: WikiAnalysisResult {
    pages_to_create: [{ slug, title, topics }]
    pages_to_update: [{ slug, reason }]
    entities: [{ name, type }]  ← người, địa điểm, khái niệm quan trọng
}
```

Dùng để định hướng cho WikiAgent — agent biết trước nên tạo bao nhiêu trang.

### WikiAgent — tool-calling loop (tối đa 50 bước)

```
System prompt quy định:
  - Viết bằng ngôn ngữ của tài liệu nguồn
  - Tối thiểu 80 từ mỗi trang
  - Dùng [[slug]] để liên kết trang khác
  - Tránh marketing, chỉ giữ nội dung thực chất
  - Mỗi trang = 1 chủ đề cụ thể

Vòng lặp:
  Bước 1: LLM đọc wiki index, hiểu context
  Bước 2-N: LLM gọi tools để đọc/tạo/sửa trang
  Bước cuối: LLM gọi finish()
```

Sau mỗi `create_page` hoặc `update_page`:
1. Gọi `EmbeddingProvider.EmbedAsync(title + "\n" + summary)`
2. Gọi `ChromaService.UpsertAsync(collection, slug, embedding)`
3. Gọi `WikiService.RefreshLinksAsync(slug)` → parse `[[link]]` trong content → update `wiki_links`

**File liên quan:**
- `AI/ILlmProvider.cs` — interfaces
- `AI/ProviderRegistry.cs` — provider factory
- `AI/WikiAnalyzer.cs` — pre-analysis
- `AI/WikiAgent.cs` — agent loop
- `AI/Providers/*.cs` — implementations
- `Services/ChromaService.cs` — vector operations

---

## 9. Frontend — Vue 3 routing & state

### State (Pinia store)

```typescript
// src/stores/auth.ts — DUY NHẤT store hiện tại
{
  token: string | null,          // từ localStorage
  employee: EmployeeDto | null,  // profile sau khi login
  isLoggedIn: computed,          // !!token
  isAdmin: computed,             // employee.role === 'admin'
  
  login(email, password),       // POST /api/auth/login → lưu token
  fetchMe(),                    // GET /api/auth/me → load profile
  logout()                      // xóa token + redirect
}
```

### Navigation Guard

```typescript
// router/index.ts
router.beforeEach(async (to) => {
  const auth = useAuthStore()
  if (!to.meta.public && !auth.isLoggedIn) return '/login'  // chưa login → login page
  if (to.meta.public && auth.isLoggedIn) return '/'         // đã login → home
  if (to.meta.adminOnly && !auth.isAdmin) return '/'        // không phải admin → home
  if (auth.isLoggedIn && !auth.employee) await auth.fetchMe() // load profile nếu chưa có
})
```

### API Layer

```typescript
// src/api/axios.ts — Axios instance dùng chung
// BaseURL: /api  → Vite proxy đến http://localhost:5000/api
// Interceptor request: tự thêm Authorization: Bearer {token}
// Interceptor response: nếu 401 → xóa token, redirect /login

// Mỗi domain có file riêng:
// api/auth.ts     → authApi.login(), .me(), .changePassword()
// api/wiki.ts     → wikiApi.list(), .get(), .create(), .search()
// api/index.ts    → sourcesApi, draftsApi, projectsApi, adminApi, rbacApi
```

### Component hierarchy

```
App.vue (<RouterView />)
    │
    ├── /login  → LoginView.vue
    │
    └── /       → AppLayout.vue
                    ├── <aside> Sidebar (NavGroup + NavItem)
                    └── <main> <RouterView />
                                ├── /wiki           → WikiBrowserView.vue
                                │   └── /wiki/:slug →   WikiPageView.vue (nested)
                                ├── /sources        → SourcesView.vue
                                ├── /projects       → ProjectsView.vue
                                ├── /projects/:id   → ProjectDetailView.vue
                                ├── /drafts         → DraftsView.vue
                                ├── /admin/*        → admin/*.vue
                                └── /profile        → ProfileView.vue
```

---

## 10. Config & Environment

### appsettings.json (backend)

```json
{
  "ConnectionStrings": {
    "Default": "Data Source=javis.db"
  },
  "Jwt": {
    "SecretKey": "CHANGE_THIS_IN_PRODUCTION_32_CHARS_MIN",
    "Issuer": "JavisApi",
    "Audience": "JavisApp",
    "ExpiryDays": 7
  },
  "ChromaDB": {
    "BaseUrl": "http://localhost:8001"
  },
  "Storage": {
    "Provider": "local",
    "LocalPath": "uploads"
  },
  "Worker": {
    "MaxJobs": 3
  },
  "DefaultAdmin": {
    "Email": "admin@javis.local",
    "Password": "admin123",
    "Name": "Administrator"
  }
}
```

### AI Settings — lưu trong DB (bảng `app_config`)

Admin cấu hình qua UI `/admin/settings`, không phải appsettings.json:

| Key | Mô tả |
|---|---|
| `llm_provider` | `anthropic` \| `openai` \| `google` |
| `embedding_provider` | `openai` \| `google` |
| `llm_model` | e.g. `claude-3-5-haiku-20241022` |
| `anthropic_api_key` | (encrypted AES-256) |
| `openai_api_key` | (encrypted AES-256) |
| `google_api_key` | (encrypted AES-256) |

### Vite proxy (frontend)

```typescript
// vite.config.ts
proxy: {
  '/api': { target: 'http://localhost:5000' },  // → backend API
  '/mcp': { target: 'http://localhost:5000' },  // → MCP endpoint
}
```

---

## 11. Quy ước code

### Backend (C#)

| Quy ước | Ví dụ |
|---|---|
| Controller chỉ điều phối | Không query DB trực tiếp, gọi Service |
| Service nhận DbContext qua DI | `Scoped` lifetime |
| DTO != Entity | `EmployeeDto` khác `Employee` model |
| Async/await xuyên suốt | Không dùng `.Result` hay `.Wait()` |
| Nullable reference types | Bật sẵn — luôn check null |
| JSON columns | Dùng `[NotMapped]` property + serializer trong getter/setter |
| Enum trong DB | Lưu string (không lưu int) |

**Thêm 1 endpoint mới:**
1. Thêm DTO vào `DTOs/` nếu cần shape mới
2. Thêm method vào Service liên quan
3. Thêm action vào Controller (hoặc tạo Controller mới)
4. Test qua Swagger: `http://localhost:5000/swagger`

### Frontend (Vue 3 + TypeScript)

| Quy ước | Ví dụ |
|---|---|
| Composition API + `<script setup>` | Không dùng Options API |
| Type API calls | `api.get<WikiPage[]>(...)` có generic type |
| Không query API trong component | Dùng qua `api/` files |
| Tailwind utilities | Không viết custom CSS nếu Tailwind đủ |
| `v-for` luôn có `:key` | Dùng ID, không dùng index |

**Thêm 1 view mới:**
1. Thêm method vào `src/api/index.ts`
2. Tạo `src/views/XxxView.vue`
3. Thêm route vào `src/router/index.ts`
4. Thêm `<NavItem>` vào `AppLayout.vue` nếu cần

---

## 12. Hướng dẫn thêm tính năng mới

### Ví dụ: Thêm "Tags" cho wiki page

**Bước 1: Model** (`Models/Wiki.cs`)
```csharp
[Column("tags_json")]
public string TagsJson { get; set; } = "[]";

[NotMapped]
public List<string> Tags
{
    get => JsonSerializer.Deserialize<List<string>>(TagsJson) ?? [];
    set => TagsJson = JsonSerializer.Serialize(value);
}
```

**Bước 2: Migration**
```bash
cd backend/JavisApi
dotnet ef migrations add AddWikiTags
dotnet ef database update
```

**Bước 3: DTO** (`DTOs/Wiki/WikiDtos.cs`)
```csharp
public record WikiPageDto(/* ... existing ... */, List<string> Tags);
public record UpdateWikiPageRequest(/* ... */, List<string>? Tags);
```

**Bước 4: Service** (`Services/WikiService.cs`)
```csharp
// Trong UpdatePageAsync():
if (request.Tags is not null) page.Tags = request.Tags;
```

**Bước 5: Frontend API** (`src/api/wiki.ts`)
```typescript
// Thêm tags vào WikiPage interface và request types
```

**Bước 6: UI** — thêm tag input vào wiki editor view

---

## Chạy dự án

```bash
# 1. Backend
cd backend/JavisApi
dotnet run --urls "http://localhost:5000"
# → Swagger: http://localhost:5000/swagger
# → Hangfire: http://localhost:5000/hangfire

# 2. ChromaDB (cần Docker)
docker run -p 8001:8000 chromadb/chroma:latest

# 3. Frontend
cd frontend
npm run dev
# → http://localhost:5173

# Login mặc định:
# Email: admin@javis.local
# Password: admin123
```

> **Lưu ý:** Nếu không chạy ChromaDB, tính năng semantic search và embedding sẽ không hoạt động, nhưng full-text search và toàn bộ CRUD vẫn hoạt động bình thường.
