# Frontend — AI Agent Instructions

## Stack
Vue 3.5 + TypeScript + Vite 8 + Pinia 3 + Vue Router 4 + Tailwind CSS 4 + Axios + lucide-vue-next + markdown-it

## Non-obvious facts about this setup

### Tailwind CSS 4 — khác Tailwind 3
- Import: `@import 'tailwindcss'` trong `style.css` (không phải `@tailwind base/components/utilities`)
- Config: trong `vite.config.ts` qua plugin `@tailwindcss/vite`, không có `tailwind.config.js`
- Custom tokens: dùng `@theme` block trong CSS, không phải `theme.extend` trong JS config
- Custom colors defined: `sienna` (#c2652a), `sienna-dark` (#a0541f), `linen` (#faf5ee), `rose` (#8c3c3c)

### Vite proxy
`/api/*` và `/mcp/*` được proxy đến `http://localhost:5000`. Không cần CORS trong dev.

### Pinia 3 — khác Pinia 2
- Store dùng setup syntax (`defineStore` với function), không dùng options syntax
- `storeToRefs` vẫn hoạt động bình thường

## Quy tắc bắt buộc

1. **`<script setup lang="ts">`** — luôn dùng, không dùng Options API
2. **Không gọi axios trực tiếp trong component** — mọi API call qua `src/api/*.ts`
3. **Generic type cho API** — `api.get<Type>(...)` không dùng `any`
4. **`v-for` phải có `:key`** — dùng ID, không dùng index mảng
5. **Không inline styles** — dùng Tailwind classes

## Cấu trúc thư mục

```
src/
├── api/
│   ├── axios.ts      ← Axios instance, JWT interceptor, 401 handler
│   ├── auth.ts       ← authApi: login, me, changePassword
│   ├── wiki.ts       ← wikiApi: list, get, create, update, delete, search
│   └── index.ts      ← sourcesApi, draftsApi, projectsApi, adminApi, rbacApi
├── stores/
│   └── auth.ts       ← token, employee, isLoggedIn, isAdmin, login(), logout()
├── router/
│   └── index.ts      ← routes + beforeEach guard (public / adminOnly meta)
├── components/
│   └── layout/
│       ├── AppLayout.vue   ← Sidebar (stone-900) + <RouterView>
│       ├── NavGroup.vue    ← Section header trong sidebar
│       └── NavItem.vue     ← Menu item (RouterLink)
└── views/
    ├── LoginView.vue
    ├── ProfileView.vue
    ├── SourcesView.vue
    ├── DraftsView.vue
    ├── ProjectsView.vue
    ├── ProjectDetailView.vue
    ├── wiki/
    │   ├── WikiBrowserView.vue  ← Sidebar panel: danh sách + search
    │   └── WikiPageView.vue     ← Markdown render (dùng markdown-it)
    └── admin/
        ├── EmployeesView.vue
        ├── DepartmentsView.vue
        ├── RolesView.vue
        ├── KnowledgeTypesView.vue
        ├── SettingsView.vue    ← AI provider settings (sensitive)
        └── AuditView.vue
```

## Pattern view chuẩn

```vue
<template>
  <div class="p-8 max-w-4xl mx-auto">
    <div class="flex items-center justify-between mb-6">
      <h1 class="font-serif text-3xl text-stone-800">Tiêu đề</h1>
      <button @click="showModal = true"
        class="flex items-center gap-2 bg-sienna text-white px-4 py-2 rounded-lg text-sm hover:bg-sienna-dark">
        <Plus :size="16" /> Thêm mới
      </button>
    </div>

    <!-- List -->
    <!-- Modal -->
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted, reactive } from 'vue'
import { Plus } from 'lucide-vue-next'
import { rbacApi } from '@/api/index'

const items = ref<object[]>([])
const showModal = ref(false)
const form = reactive({ name: '', description: '' })

async function load() { items.value = (await rbacApi.getXxx()).data }
async function create() { await rbacApi.createXxx(form); showModal.value = false; await load() }

onMounted(load)
</script>
```

## Design tokens

| Token | Màu | Dùng cho |
|---|---|---|
| `bg-sienna` / `text-sienna` | #c2652a | Primary action, active state |
| `hover:bg-sienna-dark` | #a0541f | Button hover |
| `bg-linen` | #faf5ee | Page background |
| `font-serif` | EB Garamond | Heading h1-h3 |
| `font-sans` | Manrope | Body text (default) |
| `text-stone-800` | — | Primary text |
| `text-stone-400` | — | Muted/secondary text |
| `border-stone-200` | — | Card borders |
| `bg-white rounded-xl border border-stone-200` | — | Card container |

## Router meta

```typescript
{ path: '/admin/xxx', meta: { adminOnly: true } }  // chỉ admin
{ path: '/login', meta: { public: true } }          // không cần login
// không có meta → cần login, mọi role đều vào được
```

## Thêm view mới (checklist)

```
[ ] 1. API calls → src/api/index.ts
[ ] 2. Tạo src/views/{Name}View.vue
[ ] 3. Route → src/router/index.ts
[ ] 4. NavItem → AppLayout.vue (nếu cần link sidebar)
[ ] 5. npx tsc --noEmit để check TypeScript
```

## Không làm

- Không dùng `<style>` scoped CSS nếu Tailwind làm được
- Không import thêm CSS file mới
- Không dùng `console.log` trong production code
- Không dùng `document.querySelector` hay DOM manipulation — dùng Vue refs
- Không dùng `router.push` trong template — dùng `<RouterLink>`
