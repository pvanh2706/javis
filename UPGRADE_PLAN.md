# Javis — Kế Hoạch Nâng Cấp Toàn Diện

> Mục tiêu: Đưa Javis từ MVP lên production-grade, vượt Arkon về tính năng và hiệu năng.

---

## Tổng quan khoảng cách hiện tại

| Tính năng | Arkon | Javis hiện tại | Mức độ ưu tiên |
|---|---|---|---|
| Wiki revision history | ✅ | ❌ | 🔴 Cao |
| Image understanding (Vision AI) | ✅ | ❌ | 🔴 Cao |
| Real-time progress ingestion | ✅ | ❌ | 🔴 Cao |
| Document outline building | ✅ | ❌ | 🟡 Trung bình |
| Graph navigation (wikilinks) | ✅ | ❌ | 🟡 Trung bình |
| Distributed background jobs | ✅ Redis+arq | ⚠️ In-memory | 🟡 Trung bình |
| Full-text search chất lượng cao | ✅ GIN Index | ⚠️ SQLite LIKE | 🟡 Trung bình |
| Skills module | ✅ | ❌ | 🟢 Thấp |
| Real-time notifications | ❌ | ❌ | 🔴 Cao (mới) |
| Production storage (S3/MinIO) | ✅ MinIO | ❌ Local only | 🟡 Trung bình |
| Audit trail đầy đủ | ⚠️ | ⚠️ | 🟡 Trung bình |
| Docker / deploy | ✅ | ❌ | 🟡 Trung bình |

---

## Lựa chọn công nghệ tối ưu

### Database
- **Dev**: Giữ SQLite — zero config
- **Production**: Chuyển sang **PostgreSQL 16** qua EF Core provider (`Npgsql.EntityFrameworkCore.PostgreSQL`)
  - Lý do: Full-text search GIN index, better concurrency, pgvector có thể thay ChromaDB
  - EF Core hỗ trợ switch provider bằng 1 dòng config — không cần viết lại code

### Vector Search
- **Giữ ChromaDB** cho dev (zero config, REST API đơn giản)
- **Production option**: **Qdrant** — có .NET client chính thức, hỗ trợ filtering tốt hơn, hoặc dùng **pgvector** nếu đã có PostgreSQL

### Background Jobs
- **Dev**: Hangfire in-memory (hiện tại)
- **Production**: **Hangfire + PostgreSQL storage** — không cần Redis, tận dụng DB sẵn có
  - Package: `Hangfire.PostgreSql`
  - Hoặc nếu cần distributed cao: `Hangfire + Redis` (package `Hangfire.Pro.Redis`)

### Real-time
- **SignalR** (built-in ASP.NET Core) — zero dependency, WebSocket/long-polling fallback tự động
- Dùng cho: progress ingestion, notification draft được approve/reject

### File Storage
- **Interface đã có** (`IStorageService`) — chỉ cần thêm implementation
- **Dev**: LocalStorageService (hiện tại)
- **Production**: `AWSSDK.S3` hoặc `Minio` .NET client — cùng S3 API

### Full-text Search
- **Dev**: SQLite FTS5 (tốt hơn LIKE rất nhiều, cùng SQLite)
- **Production**: PostgreSQL `tsvector` + GIN index — qua EF Core raw query

### Frontend bổ sung
- **Vue Flow** — visualize wikilink graph (drag & drop, zoom)
- **@vueuse/core** — composables tiện ích (useWebSocket, useIntersectionObserver...)
- **Tiptap** — rich text editor cho wiki draft (thay textarea thuần)

---

## Phase 1 — Real-time Progress & Notifications *(1-2 ngày)*

### Mục tiêu
Người dùng upload tài liệu → thấy thanh tiến độ cập nhật real-time thay vì chờ refresh.

### Backend

**1.1 Thêm SignalR**
```csharp
// Program.cs
builder.Services.AddSignalR();
app.MapHub<ProgressHub>("/hubs/progress");
```

**1.2 Tạo `Hubs/ProgressHub.cs`**
```csharp
public class ProgressHub : Hub
{
    // Client join group theo sourceId
    public async Task JoinSource(string sourceId) =>
        await Groups.AddToGroupAsync(Context.ConnectionId, $"source:{sourceId}");
}
```

**1.3 Inject `IHubContext<ProgressHub>` vào Jobs**

Trong `IngestFileJob.cs` và `CompileWikiJob.cs`:
```csharp
// Thay vì chỉ update DB, broadcast cả SignalR
await _hub.Clients.Group($"source:{sourceId}")
    .SendAsync("ProgressUpdate", new { percent = 30, message = "Extracting text..." });
```

**1.4 Thêm `Progress` fields vào `Source` model**
```csharp
public int ProgressPercent { get; set; }
public string? ProgressMessage { get; set; }
```

### Frontend

**1.5 `src/composables/useSourceProgress.ts`**
```typescript
import * as signalR from '@microsoft/signalr'

export function useSourceProgress(sourceId: string, onUpdate: (p: Progress) => void) {
  const connection = new signalR.HubConnectionBuilder()
    .withUrl('/hubs/progress').withAutomaticReconnect().build()
  connection.on('ProgressUpdate', onUpdate)
  connection.start().then(() => connection.invoke('JoinSource', sourceId))
  onUnmounted(() => connection.stop())
}
```

**1.6 ProgressBar component trong SourcesView**
- Hiển thị `█████░░░ 45% — Compiling wiki...` per source row

---

## Phase 2 — Wiki Revision History *(1 ngày)*

### Mục tiêu
Mỗi lần sửa wiki → snapshot immutable. Xem lịch sử, so sánh diff, rollback.

### Backend

**2.1 Model `WikiPageRevision` (đã có trong schema, cần implement logic)**
```csharp
public class WikiPageRevision
{
    public Guid Id { get; set; }
    public Guid WikiPageId { get; set; }
    public int RevisionNumber { get; set; }
    public string Content { get; set; } = "";
    public string Title { get; set; } = "";
    public string ChangeSummary { get; set; } = "";
    public Guid AuthorId { get; set; }
    public DateTime CreatedAt { get; set; }
}
```

**2.2 WikiService — snapshot khi update**
```csharp
// Trong UpdatePageAsync, trước khi save:
var revision = new WikiPageRevision {
    WikiPageId = page.Id,
    RevisionNumber = await GetNextRevisionNumber(page.Id),
    Content = page.Content,         // nội dung CŨ trước khi update
    Title = page.Title,
    ChangeSummary = request.ChangeSummary ?? "AI update",
    AuthorId = actorId,
    CreatedAt = DateTime.UtcNow
};
_db.WikiPageRevisions.Add(revision);
```

**2.3 Endpoints**
```
GET /api/wiki/{slug}/revisions          → list all revisions
GET /api/wiki/{slug}/revisions/{revNum} → content of specific revision
POST /api/wiki/{slug}/revisions/{revNum}/restore → rollback
```

### Frontend

**2.4 Diff viewer** — dùng thư viện `diff-match-patch` (port JS), hiển thị side-by-side:
- Xanh = thêm mới, Đỏ = đã xóa
- Trong `WikiPageView.vue` thêm tab "History"

---

## Phase 3 — Image Understanding (Vision AI) *(2-3 ngày)*

### Mục tiêu
PDF có sơ đồ, biểu đồ, ảnh chụp → AI đọc và caption → WikiAgent biết nội dung ảnh.

### Backend

**3.1 Mở rộng `ILlmProvider`**
```csharp
public interface IVisionProvider
{
    Task<string> DescribeImageAsync(byte[] imageBytes, string mimeType,
        string prompt = "Describe this image in detail for a knowledge base.",
        CancellationToken ct = default);
}
```

**3.2 Implement trong AnthropicProvider**
```csharp
// Claude: dùng message với content array [image_source, text]
// OpenAI: dùng content array với image_url
// Google: dùng inlineData part
```

**3.3 Mở rộng `KbService` — `ExtractWithImages()`**
- Dùng `PdfPig` extract từng trang
- Với mỗi trang: render sang ảnh (dùng `PDFium.NET` hoặc `Docnet.Core`)
- Gọi Vision provider → caption
- Chèn caption vào text: `[IMAGE: {caption}]`

**3.4 `IngestFileJob` — thêm bước captioning**
```
Step 1 (10%): Download
Step 2 (25%): Extract text + page boundaries  
Step 3 (50%): Caption images ← MỚI
Step 4 (60%): Build document outline ← MỚI
Step 5 (70%): WikiAnalyzer pre-analysis
Step 6 (95%): WikiAgent compilation
Step 7 (100%): Done
```

### Thư viện cần thêm
```xml
<PackageReference Include="Docnet.Core" Version="2.6.0" />
<!-- Hoặc dùng external process: pdftoppm (Poppler) qua Process.Start -->
```

---

## Phase 4 — Document Outline & Source Navigation *(1 ngày)*

### Mục tiêu
WikiAgent biết cấu trúc heading của tài liệu gốc → tạo wiki tốt hơn.

### Backend

**4.1 Thêm `OutlineJson` vào `Source`**
```csharp
public string? OutlineJson { get; set; } // JSON tree of headings
public string? PageOffsetsJson { get; set; } // char offset per page
```

**4.2 `KbService.BuildOutline()`**
```csharp
public record OutlineNode(string Title, int Level, int CharOffset, List<OutlineNode> Children);

// Parse heading patterns: # ## ### từ markdown, hoặc font-size heuristic từ PDF
```

**4.3 Tool `read_source_outline` trong WikiAgent**
```csharp
case "read_source_outline":
    var outline = JsonSerializer.Deserialize<OutlineNode[]>(source.OutlineJson ?? "[]");
    return RenderOutlineTree(outline);
```

WikiAgent dùng outline để lập kế hoạch trước khi bắt đầu tạo trang.

---

## Phase 5 — Graph Navigation (Wikilink Graph) *(1 ngày)*

### Mục tiêu
Từ một trang wiki, có thể duyệt backlinks, outlinks, và xem toàn bộ knowledge graph.

### Backend

**5.1 Endpoints**
```
GET /api/wiki/{slug}/links          → { outlinks: [...], backlinks: [...] }
GET /api/wiki/{slug}/neighborhood   → graph 2 hops xung quanh trang
```

**5.2 `WikiService.GetNeighborhood()`**
```csharp
// Recursive CTE trong SQLite:
WITH RECURSIVE graph(slug, depth) AS (
  SELECT 'target-slug', 0
  UNION ALL
  SELECT wl.target_slug, g.depth + 1
  FROM wiki_links wl JOIN graph g ON wl.source_slug = g.slug
  WHERE g.depth < 2
)
SELECT DISTINCT slug FROM graph;
```

### Frontend

**5.3 Graph Viewer** — dùng **Vue Flow** (`@vue-flow/core`)

```typescript
// Nodes = wiki pages, Edges = wikilinks
// Layout tự động với dagre
// Click node → navigate to page
// Zoom/pan/search trong graph
```

Thêm button "View Graph" trong `WikiBrowserView.vue` → mở panel graph bên phải.

---

## Phase 6 — PostgreSQL + Production Storage *(1 ngày)*

### Mục tiêu
Từ SQLite + LocalStorage → PostgreSQL + S3-compatible (có thể dùng Cloudflare R2).

### Backend

**6.1 Switch Database Provider**
```csharp
// appsettings.Production.json
{ "Database": { "Provider": "postgres", "ConnectionString": "..." } }

// Program.cs — conditional
if (dbProvider == "postgres")
    options.UseNpgsql(connectionString)
          .UseVectorExtension(); // pgvector thay ChromaDB
else
    options.UseSqlite(connectionString);
```

**6.2 `S3StorageService` implements `IStorageService`**
```csharp
public class S3StorageService : IStorageService
{
    // AWS SDK v3: AWSSDK.S3
    // Tương thích Cloudflare R2, MinIO, Backblaze B2
    // Presigned URL 24h cho download
    // Multipart upload cho file lớn
}
```

**6.3 Cấu hình qua env vars**
```
STORAGE_PROVIDER=s3
AWS_ENDPOINT_URL=https://xxx.r2.cloudflarestorage.com
AWS_ACCESS_KEY_ID=...
AWS_SECRET_ACCESS_KEY=...
AWS_BUCKET_NAME=javis-uploads
```

**6.4 Hangfire chuyển sang PostgreSQL storage**
```csharp
config.UsePostgreSqlStorage(connectionString);
// → Job history persist sau restart, hỗ trợ multiple worker instances
```

---

## Phase 7 — Full-Text Search Nâng Cao *(1 ngày)*

### Mục tiêu
Thay SQLite LIKE → SQLite FTS5 (dev) / PostgreSQL tsvector (prod) — nhanh hơn 100x.

### SQLite FTS5 (dev)

**7.1 Migration — tạo FTS virtual table**
```sql
CREATE VIRTUAL TABLE wiki_pages_fts USING fts5(
    slug, title, content, summary,
    content='wiki_pages', content_rowid='rowid'
);
-- Trigger sync
CREATE TRIGGER wiki_pages_ai AFTER INSERT ON wiki_pages BEGIN
    INSERT INTO wiki_pages_fts(rowid, slug, title, content, summary)
    VALUES (new.rowid, new.slug, new.title, new.content, new.summary);
END;
```

**7.2 Query**
```csharp
// EF Core raw SQL
var results = await _db.Database.SqlQuery<WikiFtsResult>(
    $"SELECT slug, title, snippet(wiki_pages_fts, 2, '<b>', '</b>', '...', 32) as excerpt " +
    $"FROM wiki_pages_fts WHERE wiki_pages_fts MATCH {query} ORDER BY rank"
).ToListAsync();
```

### PostgreSQL (prod)
- Thêm `tsvector` column, GIN index, `ts_rank` scoring
- Hỗ trợ stop words, stemming tiếng Anh/Việt

---

## Phase 8 — Skills Module *(2 ngày)*

### Mục tiêu
Mỗi employee có Skills (kỹ năng) → có thể tìm người theo skill → wiki skill được auto-generated.

### Backend

**8.1 Models** (đã có skeleton trong `Skill.cs`)
```csharp
Skill { Id, Name, Slug, Description, Status }
SkillVersion { Id, SkillId, Content, CreatedAt }
SkillDepartment { SkillId, DepartmentId }
EmployeeSkill { EmployeeId, SkillId, Level, VerifiedAt }
```

**8.2 Endpoints**
```
GET    /api/skills                      → list skills
POST   /api/skills                      → create skill
GET    /api/skills/{slug}               → skill detail + who has it
POST   /api/skills/{id}/compile         → trigger AI compilation
GET    /api/employees/{id}/skills       → employee skill profile
POST   /api/employees/{id}/skills       → add skill to employee
```

**8.3 SkillCompileJob (Hangfire)**
- Tương tự WikiAgent nhưng focus vào mô tả kỹ năng, cấp độ, use cases
- Kết quả → `SkillVersion` → liên kết vào wiki page tương ứng

---

## Phase 9 — Frontend Nâng Cấp *(2-3 ngày)*

### 9.1 Wiki Editor với Tiptap
- Thay `<textarea>` bằng **Tiptap** editor (ProseMirror based)
- Hỗ trợ: Markdown shortcuts, `[[wikilink]]` autocomplete, table, code block
- Preview real-time markdown

### 9.2 Wikilink Autocomplete
```typescript
// Khi gõ [[, hiện dropdown search pages
// Dùng @tiptap/extension-mention
```

### 9.3 Knowledge Graph View
```typescript
// @vue-flow/core + dagre-d3
// Visualize toàn bộ wiki như mind map
// Filter theo knowledge type / department
```

### 9.4 Semantic Search UI
- Thêm toggle "Semantic" / "Keyword" trong search
- Hiển thị relevance score
- Highlight đoạn match

### 9.5 Real-time Notifications (SignalR)
- Toast notification khi draft được approve/reject
- Badge counter trên menu "Drafts"
- Live update source status mà không cần refresh

---

## Thứ tự thực hiện đề xuất

```
Tuần 1:
  ├── Phase 1: SignalR progress (2 ngày)
  └── Phase 2: Wiki revisions (1 ngày) + Phase 7: FTS5 (1 ngày)

Tuần 2:
  ├── Phase 3: Vision AI (2 ngày)
  └── Phase 4: Document outline (1 ngày) + Phase 5: Graph nav (1 ngày)

Tuần 3:
  ├── Phase 6: PostgreSQL + S3 (1 ngày)
  └── Phase 9: Frontend upgrades (3 ngày)

Tuần 4:
  └── Phase 8: Skills module (2 ngày) + testing + Docker
```

---

## Docker Compose cuối cùng (production-ready)

```yaml
version: '3.9'
services:
  api:
    build: ./backend
    environment:
      - Database__Provider=postgres
      - Storage__Provider=s3
      - Hangfire__Storage=postgres
    depends_on: [postgres, chromadb]

  worker:
    build: ./backend
    command: ["dotnet", "JavisApi.dll", "--worker-only"]
    # Scale: docker-compose up --scale worker=3

  frontend:
    build: ./frontend
    ports: ["80:80"]

  postgres:
    image: pgvector/pgvector:pg16
    volumes: [pg_data:/var/lib/postgresql/data]

  chromadb:
    image: chromadb/chroma:latest
    volumes: [chroma_data:/chroma/chroma]

volumes:
  pg_data:
  chroma_data:
```

---

## Tổng kết

Sau khi hoàn thành tất cả phases, Javis sẽ:

| Tiêu chí | Arkon | Javis (sau nâng cấp) |
|---|---|---|
| Vision AI | ✅ | ✅ |
| Real-time progress | ✅ | ✅ **+ SignalR toast** |
| Revision history | ✅ | ✅ **+ diff viewer** |
| Graph navigation | ✅ 3-hop | ✅ **+ visual graph UI** |
| Full-text search | ✅ GIN | ✅ **FTS5/tsvector** |
| Wiki editor | ❌ textarea | ✅ **Tiptap rich editor** |
| Skills module | ✅ | ✅ |
| Production storage | ✅ MinIO | ✅ **S3-compatible** |
| Distributed jobs | ✅ Redis | ✅ **Hangfire+Postgres** |
| Wikilink autocomplete | ❌ | ✅ **Mới** |
| Knowledge graph UI | ❌ | ✅ **Vue Flow — Mới** |
| Deployment | ✅ Docker | ✅ **Docker** |
