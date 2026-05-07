# Backend — AI Agent Instructions

## Stack
ASP.NET Core 8, C# 12, Entity Framework Core 8 + SQLite, Hangfire (in-memory), ChromaDB (external HTTP), Anthropic SDK 5.x, Azure.AI.OpenAI 2.x

## Project structure
```
JavisApi/
├── Program.cs          ← DI registration, middleware, seed admin
├── Controllers/        ← HTTP endpoints (thin, delegate to Services)
├── Services/           ← Business logic (Scoped lifetime)
├── AI/                 ← WikiAnalyzer, WikiAgent, ProviderRegistry, Providers/
├── Jobs/               ← Hangfire: IngestFileJob → CompileWikiJob
├── Models/             ← EF Core entities (1 file per domain)
├── DTOs/               ← Request/Response records (not entities)
└── Data/AppDbContext.cs
```

## Architecture rules — non-negotiable

### 1. Layer separation
```
Controller → Service → AppDbContext
```
Controllers: validate input, check permission, call service, return result.
Services: all business logic, all DB queries, all external calls.

### 2. Dependency injection
All services are registered in `Program.cs` and injected via constructor.
- `AppDbContext` — `Scoped`
- All `*Service` classes — `Scoped`
- `IStorageService` (LocalStorageService) — `Singleton`
- `WikiAgent`, `WikiAnalyzer` — `Scoped`

**Never use `new Service(...)` — always inject.**

### 3. AI provider resolution
```csharp
// ✅ Always
var llm = await _registry.GetLlmAsync();
var emb = await _registry.GetEmbeddingAsync();
// ❌ Never
var llm = new AnthropicProvider("key");
```
`ProviderRegistry` reads `llm_provider` key from `app_config` DB table (encrypted via `ConfigService`).

### 4. Permission check pattern
```csharp
// In every controller action that accesses protected resources:
var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
var employee = await _db.Employees
    .Include(e => e.CustomRole).Include(e => e.Department)
    .FirstOrDefaultAsync(e => e.Id == userId);
if (employee is null) return Unauthorized();
if (!_permissions.CanEditWiki(employee)) return Forbid();
```

### 5. Wiki scope — always both fields
```csharp
// Every wiki/source operation needs both scopeType and scopeId
string scopeType = "global";     // or "project"
Guid? scopeId = null;            // null for global, project.Id for project
await _wikiService.GetBySlugAsync(slug, scopeType, scopeId);
```

### 6. JSON columns
Models with list/array data store as JSON TEXT in SQLite:
```csharp
[Column("permissions_json")]
public string PermissionsJson { get; set; } = "[]";

[NotMapped]
public List<string> Permissions
{
    get => JsonSerializer.Deserialize<List<string>>(PermissionsJson) ?? [];
    set => PermissionsJson = JsonSerializer.Serialize(value);
}
```

### 7. Embeddings are external
`WikiPage` has NO embedding column. After create/update:
```csharp
var vec = await embeddingProvider.EmbedAsync($"{title}\n{summary}", ct);
var collection = scopeType == "global"
    ? "wiki_pages_global"
    : $"wiki_pages_project_{scopeId}";
await _chroma.UpsertAsync(collection, slug, vec, metadata, ct);
```

### 8. Background jobs
```csharp
// Enqueue from controller (IBackgroundJobClient injected)
_jobClient.Enqueue<IngestFileJob>(j => j.ExecuteAsync(source.Id));

// Jobs use IServiceScopeFactory for scoped services
// See: Jobs/IngestFileJob.cs, Jobs/CompileWikiJob.cs
```

### 9. Encrypted config
`app_config` table values are AES-256 encrypted:
```csharp
// ✅
var apiKey = await _config.GetSettingAsync("anthropic_api_key");
// ❌ Never query app_config.Value directly
```

### 10. Audit logging
All create/update/delete operations must log:
```csharp
await _audit.LogAsync("wiki_page_created", employee.Id, "wiki_page", page.Id.ToString(), title);
```

## Key service responsibilities

| Service | What it owns |
|---|---|
| `AuthService` | Login validation, JWT generation, BCrypt, MCP token gen |
| `WikiService` | Wiki CRUD, full-text search, link refresh, draft workflow |
| `PermissionEngine` | All RBAC checks (IsAdmin, HasPermission, FilterSources...) |
| `ChromaService` | ChromaDB HTTP: upsert, query, delete collections |
| `ConfigService` | Read/write encrypted app_config (AES-256, key = SHA256 of JWT secret) |
| `KbService` | Text extraction: PDF (PdfPig), Word (OpenXml), HTML (HtmlAgilityPack), URL |
| `McpAuthService` | Verify `ark_xxx` tokens, update LastConnected |
| `AuditService` | Append-only audit log writer |
| `StorageService` | Upload/download files (dev: local disk, uploads/ folder) |

## WikiAgent tool list (8 tools, 50 step limit)

| Tool | Action |
|---|---|
| `read_wiki_index` | List all wiki pages (slug, title, summary) in scope |
| `read_wiki_page` | Get full Markdown content of a page |
| `search_wiki` | Full-text search, returns top 5 |
| `read_source_excerpt` | Read chars[start..end] from source.FullText |
| `create_page` | Create new wiki page (min 80 words) + embed + parse links |
| `update_page` | Update existing page + re-embed + re-parse links |
| `append_log` | Append entry to `_log` wiki page |
| `finish` | End agent loop |

## Commands
```bash
# Build
dotnet build

# Add migration
dotnet ef migrations add <MigrationName>
dotnet ef database update

# Run
dotnet run --urls "http://localhost:5000"
# Swagger: http://localhost:5000/swagger
# Hangfire: http://localhost:5000/hangfire

# Default login: admin@javis.local / admin123
```

## Do NOT
- Do NOT add `.Result`, `.Wait()`, `.GetAwaiter().GetResult()` — always `await`
- Do NOT query DB in Controllers — use Services
- Do NOT read `app_config` directly — use `ConfigService`
- Do NOT skip permission checks on protected endpoints
- Do NOT write to `Migrations/` folder manually
- Do NOT use `dynamic` — use proper typed DTOs
- Do NOT create new `HttpClient` instances — use injected `IHttpClientFactory`
