# Javis — Agent Instructions (Codex / OpenAI Agents)

## Project overview
Javis is an internal knowledge base platform. Uploaded documents (PDF, Word, URL) are processed by AI into a structured wiki. Employees and Claude Desktop query the wiki via REST API and MCP protocol.

## Repository layout
```
backend/JavisApi/     ASP.NET Core 8 Web API (C# 12)
frontend/             Vue 3 + Vite + Pinia + Tailwind CSS 4
CLAUDE.md             Full instructions for Claude agents
CODEBASE_GUIDE.md     Architecture deep-dive for human developers
UPGRADE_PLAN.md       Planned feature roadmap
```

## Key architecture decisions you must respect

### 1. Always use the service layer
Controllers call Services. Services own all DB queries via `AppDbContext`.
Never write EF Core queries inside a Controller.

### 2. Permission check on every mutating endpoint
```csharp
var employee = await GetCurrentEmployeeAsync(); // resolves from JWT claims
if (!_permissions.CanUploadSource(employee)) return Forbid();
```

### 3. AI provider must be resolved dynamically
```csharp
// CORRECT
var llm = await _registry.GetLlmAsync();
// WRONG — never instantiate directly
var llm = new AnthropicProvider(hardcodedKey);
```

### 4. Wiki scope is always (scopeType + scopeId)
Every wiki operation requires both fields. "global" scope has `scopeId = null`.
Project-scoped wiki has `scopeType = "project"` and `scopeId = project.Id`.

### 5. Embeddings live in ChromaDB, not SQLite
`WikiPage` has no embedding column. After creating/updating a page, call:
```csharp
var vec = await embeddingProvider.EmbedAsync($"{title}\n{summary}", ct);
await _chroma.UpsertAsync(collection, slug, vec, metadata, ct);
```

### 6. JSON columns use [NotMapped] pattern
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

## Commands to run
```bash
# Build check
cd backend/JavisApi && dotnet build

# Run migrations
dotnet ef migrations add <MigrationName> && dotnet ef database update

# Run API
dotnet run --urls "http://localhost:5000"

# Run frontend
cd frontend && npm run dev

# TypeScript check
cd frontend && npx tsc --noEmit
```

## Common patterns

### Getting the current employee in a controller
```csharp
private async Task<Employee?> GetCurrentEmployeeAsync()
{
    var id = User.FindFirstValue(ClaimTypes.NameIdentifier);
    if (!Guid.TryParse(id, out var guid)) return null;
    return await _db.Employees
        .Include(e => e.CustomRole)
        .Include(e => e.Department)
        .FirstOrDefaultAsync(e => e.Id == guid);
}
```

### Enqueueing a background job
```csharp
// Inject IBackgroundJobClient via constructor
_jobClient.Enqueue<IngestFileJob>(j => j.ExecuteAsync(source.Id));
```

### Calling the wiki agent from a job
```csharp
// CompileWikiJob injects WikiAgent (Scoped via IServiceScopeFactory)
await _wikiAgent.RunAsync(sourceId, ct);
```

### Frontend API call pattern
```typescript
// All API files return AxiosResponse — unwrap with .data
const pages = (await wikiApi.list(undefined, searchQuery)).data
```

## What NOT to do
- Do not add `.Wait()`, `.Result`, or `.GetAwaiter().GetResult()` — use `await`
- Do not read `app_config.value` directly — use `ConfigService.GetSettingAsync()`
- Do not write to `Migrations/` folder manually — use EF CLI
- Do not skip audit logging for create/update/delete operations
- Do not use `dynamic` types in C# — use proper DTOs
- Do not use Options API in Vue components — always Composition API + `<script setup>`
