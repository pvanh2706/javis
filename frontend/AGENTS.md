# Frontend — Agent Instructions (Codex / OpenAI Agents)

## Stack
Vue 3.5, TypeScript, Vite 8, Pinia 3, Vue Router 4, Tailwind CSS 4 (plugin-based), Axios, lucide-vue-next, markdown-it

## Critical: Tailwind CSS 4 differences
This project uses **Tailwind CSS v4** — NOT v3. Key differences:
- No `tailwind.config.js` file
- No `@tailwind base/components/utilities` directives
- Config is done via `@theme {}` block in `src/style.css`
- Vite plugin: `@tailwindcss/vite` in `vite.config.ts`
- Custom colors are already defined: `sienna`, `sienna-dark`, `linen`, `rose`

## Non-standard setup
- `/api/*` proxied to `http://localhost:5000` (backend) — no CORS issues in dev
- Google Fonts loaded in `index.html`: EB Garamond (serif headings) + Manrope (sans body)
- Path alias `@` → `src/` configured in `vite.config.ts` and `tsconfig.json`

## Rules
1. **Composition API + `<script setup lang="ts">`** — mandatory. No Options API.
2. **API calls only via `src/api/*.ts`** — never use axios directly in components
3. **Typed responses** — always provide generic type to `api.get<Type>()`, never use `any`
4. **`v-for` requires `:key`** — use item ID, never array index
5. **State via Pinia** — `useAuthStore()` for auth state

## Standard component template
```vue
<template>
  <div class="p-8 max-w-4xl mx-auto">
    <h1 class="font-serif text-3xl text-stone-800 mb-6">Title</h1>
    <!-- content -->
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { someApi } from '@/api/index'

const items = ref<object[]>([])
async function load() { items.value = (await someApi.getAll()).data }
onMounted(load)
</script>
```

## File locations
- API calls: `src/api/index.ts`, `src/api/auth.ts`, `src/api/wiki.ts`
- Auth state: `src/stores/auth.ts` (token, employee, isAdmin, login, logout)
- Routes: `src/router/index.ts`
- Layout: `src/components/layout/AppLayout.vue`
- Views: `src/views/**/*.vue`

## Commands
```bash
npm run dev          # dev server at http://localhost:5173
npx tsc --noEmit     # TypeScript check
npm run build        # production build
```
