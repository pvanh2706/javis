# Javis — GitHub Copilot Instructions

## Project context
Javis is an internal knowledge base platform built with ASP.NET Core 8 (backend) and Vue 3 (frontend).
Documents uploaded by admins are automatically compiled into a wiki by an AI agent. Employees read the wiki; Claude Desktop queries it via MCP protocol.

---

## Backend — ASP.NET Core 8 (C# 12)

### Project location
`backend/JavisApi/` — run with `dotnet run --urls "http://localhost:5000"`

### Architecture rules

**Layers** (always follow this order):
```
HTTP Request → Controller → Service → AppDbContext (EF Core) → SQLite
```

**Never write EF queries in Controllers.** Controllers orchestrate; Services own business logic and data access.

**Services are Scoped** — injected via constructor DI. Never `new` a service manually.

**AI provider** — always resolve dynamically:
```csharp
// ✅ Correct
var llm = await _registry.GetLlmAsync(); // reads config from DB
// ❌ Wrong
var llm = new AnthropicProvider("hardcoded-key");
```

**Permission check pattern** — required at the start of every endpoint that reads or writes sensitive data:
```csharp
var employee = await GetCurrentEmployeeAsync();
if (employee is null) return Unauthorized();
if (!_permissions.CanEditWiki(employee)) return Forbid();
```

**Async only** — never use `.Result`, `.Wait()`, or blocking calls. Every method that touches I/O must be `async Task<T>`.

**Nullable reference types are enabled** — handle nulls explicitly. Use `is null` / `is not null` pattern matching.

### Key types and their purpose

| Type | File | Purpose |
|---|---|---|
| `AppDbContext` | `Data/AppDbContext.cs` | EF Core DbContext, all DbSets |
| `WikiPage` | `Models/Wiki.cs` | Wiki article entity |
| `Source` | `Models/Source.cs` | Uploaded document entity |
| `Employee` | `Models/Employee.cs` | User; role = "admin" or "employee" |
| `PermissionEngine` | `Services/PermissionEngine.cs` | All RBAC checks |
| `WikiService` | `Services/WikiService.cs` | Wiki CRUD, search, links |
| `WikiAgent` | `AI/WikiAgent.cs` | Tool-calling loop (max 50 steps) |
| `ProviderRegistry` | `AI/ProviderRegistry.cs` | Resolves LLM/embedding provider from DB |
| `IStorageService` | `Services/StorageService.cs` | File upload/download abstraction |
| `ChromaService` | `Services/ChromaService.cs` | Vector embeddings via ChromaDB HTTP |

### JSON column pattern (used in WikiPage, Source, Role)
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

### Wiki scope — ALWAYS pass both fields
Every wiki/source query must include scope:
```csharp
// scopeType: "global" or "project"
// scopeId: null (global) or project Guid
await _wikiService.GetBySlugAsync(slug, scopeType, scopeId);
```

### Background jobs (Hangfire)
```csharp
// Enqueue from controller
_jobClient.Enqueue<IngestFileJob>(j => j.ExecuteAsync(source.Id));

// Inside a job — use IServiceScopeFactory to create scoped services
using var scope = _scopeFactory.CreateScope();
var wikiAgent = scope.ServiceProvider.GetRequiredService<WikiAgent>();
```

### Adding a new endpoint
1. Add DTOs in `DTOs/{Domain}/`
2. Add method in `Services/{Domain}Service.cs`
3. Add action in `Controllers/{Domain}Controller.cs`
4. Add permission check
5. Add audit log for mutations: `await _audit.LogAsync(...)`
6. Verify: `dotnet build` then test at `http://localhost:5000/swagger`

---

## Frontend — Vue 3 + TypeScript

### Project location
`frontend/` — run with `npm run dev` (proxies `/api` to `http://localhost:5000`)

### Rules

**Always use Composition API with `<script setup>`:**
```vue
<script setup lang="ts">
import { ref, onMounted } from 'vue'
// ✅ Correct
</script>
```

**API calls go through `src/api/` files — never use axios directly in components:**
```typescript
// ✅ Correct
import { wikiApi } from '@/api/wiki'
const pages = (await wikiApi.list()).data

// ❌ Wrong
import axios from 'axios'
const res = await axios.get('/api/wiki')
```

**Use typed generics — no `any`:**
```typescript
// ✅
const res = await api.get<WikiPage[]>('/wiki')
// ❌
const res = await api.get('/wiki')
```

**Global state lives in Pinia stores (`src/stores/`):**
```typescript
const auth = useAuthStore()
if (!auth.isAdmin) return // check before rendering admin UI
```

**Tailwind classes only** — no inline styles, no custom CSS files except `style.css`.

### Key files

| File | Purpose |
|---|---|
| `src/api/axios.ts` | Axios instance + JWT interceptor + 401 redirect |
| `src/api/auth.ts` | Login, me, changePassword |
| `src/api/wiki.ts` | Wiki CRUD + search |
| `src/api/index.ts` | sources, drafts, projects, admin, rbac APIs |
| `src/stores/auth.ts` | token, employee, isLoggedIn, isAdmin |
| `src/router/index.ts` | Routes + auth guards (public, adminOnly) |
| `src/components/layout/AppLayout.vue` | Sidebar + main layout |

### Adding a new admin view
```typescript
// 1. Add API calls to src/api/index.ts
// 2. Create src/views/admin/NewFeatureView.vue
// 3. Add route in src/router/index.ts with meta: { adminOnly: true }
// 4. Add <NavItem> in AppLayout.vue under NavGroup label="Admin"
```

### Design system (Sahara theme)
- Primary: `bg-sienna` / `text-sienna` (#c2652a)
- Background: `bg-linen` (#faf5ee)
- Headings: `font-serif` (EB Garamond)
- Body: `font-sans` (Manrope)
- Danger: `text-red-500`, success: `text-green-600`

---

## Database

- **Dev**: SQLite (`backend/JavisApi/javis.db`)
- **Migrations**: `dotnet ef migrations add <Name>` then `dotnet ef database update`
- **Never edit** `Migrations/` folder manually
- `app_config` values are **AES-256 encrypted** — always use `ConfigService` to read/write

## Running the project

```bash
# Backend API
cd backend/JavisApi
dotnet run --urls "http://localhost:5000"
# Swagger: http://localhost:5000/swagger
# Hangfire: http://localhost:5000/hangfire

# Frontend
cd frontend && npm run dev
# → http://localhost:5173

# Default admin credentials
# Email: admin@javis.local  Password: admin123
```
