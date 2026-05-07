# Javis — AI Agent Instructions

## Dự án là gì
Javis là **knowledge base platform** cho doanh nghiệp. Tài liệu nội bộ (PDF, Word, URL) được upload → AI tự động tổng hợp thành wiki có cấu trúc → nhân viên và Claude Desktop truy cứu thông tin.

## Tech stack

| Layer | Công nghệ |
|---|---|
| Backend | ASP.NET Core 8, C# 12, EF Core 8 + SQLite |
| Background jobs | Hangfire (in-memory, dev) |
| Vector DB | ChromaDB (external, port 8001) |
| File storage | LocalStorageService (uploads/ folder) |
| AI providers | Anthropic SDK 5.x, Azure.AI.OpenAI 2.x, Google REST |
| Frontend | Vue 3.5, Vite 8, Pinia 3, Vue Router 4, Tailwind CSS 4, Axios |

## Cấu trúc thư mục

```
Javis/
├── backend/JavisApi/
│   ├── Program.cs              ← DI, middleware, seed
│   ├── Controllers/            ← HTTP endpoints
│   ├── Services/               ← Business logic (inject qua DI)
│   ├── AI/                     ← WikiAnalyzer, WikiAgent, Providers
│   ├── Jobs/                   ← Hangfire: IngestFileJob, CompileWikiJob
│   ├── Models/                 ← EF Core entities
│   ├── DTOs/                   ← Request/Response shapes
│   └── Data/AppDbContext.cs    ← DbContext
└── frontend/src/
    ├── api/                    ← Axios calls (axios.ts, auth.ts, wiki.ts, index.ts)
    ├── stores/auth.ts          ← Pinia: token, employee, isAdmin
    ├── router/index.ts         ← Routes + navigation guards
    ├── views/                  ← Page components
    └── components/layout/      ← AppLayout, NavGroup, NavItem
```

## Luồng cốt lõi (đọc trước khi code)

```
Upload file → SourcesController → IngestFileJob (Hangfire)
    → KbService.ExtractFromFileAsync (PdfPig/OpenXml/HtmlAgilityPack)
    → CompileWikiJob → WikiAnalyzer (1 LLM call, phân tích sơ bộ)
    → WikiAgent (tool-calling loop, tối đa 50 bước)
        tools: read_wiki_index, read_wiki_page, search_wiki,
               read_source_excerpt, create_page, update_page, finish
    → Mỗi create/update: embed → ChromaDB + parse [[wikilinks]]
    → source.Status = "ready"
```

## Quy tắc bắt buộc khi viết code

### Backend C#
1. **Controller không query DB trực tiếp** — luôn gọi Service
2. **Lifetime**: Services là `Scoped`, `IStorageService` là `Singleton`
3. **Async xuyên suốt** — không dùng `.Result`, `.Wait()`, `.GetAwaiter().GetResult()`
4. **Nullable enabled** — phải check null, dùng `?` và `!` đúng chỗ
5. **JSON columns** — dùng `[NotMapped]` property với JsonSerializer, không dùng raw string
6. **Không hardcode provider** — luôn dùng `ProviderRegistry.GetLlmAsync()` thay vì `new AnthropicProvider()`
7. **Permission check trước** — đầu mỗi endpoint phải có `_permissions.CanXxx(employee)`

### Scope trong wiki/source
- Mọi WikiPage và Source đều có `ScopeType` ("global" | "project") và `ScopeId` (nullable Guid)
- Khi tạo/query wiki, luôn truyền cả `scopeType` và `scopeId`
- Cùng `slug` có thể tồn tại ở scope khác nhau — đây là thiết kế có chủ đích

### Frontend Vue 3
1. **Composition API + `<script setup>`** — không dùng Options API
2. **Không gọi API trong component** — dùng qua `src/api/*.ts`
3. **Type generic** — `api.get<WikiPage[]>(...)` không dùng `any`
4. **`v-for` phải có `:key`** — dùng ID, không dùng index

## Các file hay bị nhầm

| Vấn đề | Đúng |
|---|---|
| `Source.MinioKey` | Tên field gốc từ Arkon, trong Javis dùng để lưu storage key (không phải MinIO) |
| `WikiPage.ContentMd` | Nội dung Markdown. Không có `Content` hay `Body` |
| `Employee.Role` | String `"admin"` hoặc `"employee"` — không phải enum |
| Embedding | **Không lưu trong SQLite** — chỉ lưu ở ChromaDB với key = slug |
| `app_config.value` | **Encrypted AES-256** — đọc/ghi qua `ConfigService`, không query thẳng |

## Thêm endpoint mới (checklist)

```
[ ] 1. DTO vào DTOs/{Domain}/{Domain}Dtos.cs
[ ] 2. Logic vào Services/{Domain}Service.cs
[ ] 3. Action vào Controllers/{Domain}Controller.cs
[ ] 4. Permission check: _permissions.CanXxx(employee)
[ ] 5. Audit log nếu mutation: _audit.LogAsync(action, actorId, ...)
[ ] 6. Test: dotnet build → http://localhost:5000/swagger
```

## Thêm view frontend mới (checklist)

```
[ ] 1. API calls vào src/api/index.ts
[ ] 2. Tạo src/views/{Name}View.vue
[ ] 3. Route vào src/router/index.ts (meta: { adminOnly: true } nếu cần)
[ ] 4. NavItem vào AppLayout.vue nếu cần sidebar link
```

## Chạy local

```bash
# Backend
cd backend/JavisApi && dotnet run --urls "http://localhost:5000"
# Swagger: http://localhost:5000/swagger | Hangfire: http://localhost:5000/hangfire

# ChromaDB (tùy chọn, cần Docker)
docker run -p 8001:8000 chromadb/chroma:latest

# Frontend
cd frontend && npm run dev  # → http://localhost:5173

# Seed admin: admin@javis.local / admin123
```

## Không làm

- Không thêm migration thủ công vào thư mục `Migrations/` — dùng `dotnet ef migrations add`
- Không gọi `_db.SaveChangesAsync()` nhiều lần trong 1 transaction — batch lại
- Không dùng `ObjectResult` hay `IActionResult` trực tiếp khi đã có typed `Ok<T>()`
- Không import CSS ngoài `style.css` trong frontend
- Không thêm `console.log` vào production code
