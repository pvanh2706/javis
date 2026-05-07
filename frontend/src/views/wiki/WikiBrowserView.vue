<template>
  <div class="flex h-full">
    <!-- Left panel: page list -->
    <aside class="w-72 border-r border-stone-200 flex flex-col bg-white">
      <div class="p-4 border-b border-stone-200">
        <div class="flex items-center gap-2 mb-3">
          <h2 class="font-serif text-xl text-stone-800 flex-1">Wiki</h2>
          <RouterLink to="/sources"
            class="text-xs text-sienna hover:underline">Sources</RouterLink>
        </div>
        <input v-model="searchQ" placeholder="Search pages…" @input="onSearch"
          class="w-full border border-stone-200 rounded-lg px-3 py-1.5 text-sm focus:outline-none focus:border-sienna" />
      </div>
      <div class="flex-1 overflow-y-auto">
        <div v-if="loading" class="p-4 text-sm text-stone-400">Loading…</div>
        <RouterLink v-for="p in pages" :key="p.slug" :to="`/wiki/${p.slug}`"
          class="block px-4 py-3 border-b border-stone-100 hover:bg-linen/60 transition-colors"
          activeClass="bg-linen border-l-2 border-sienna">
          <p class="font-medium text-sm text-stone-800 truncate">{{ p.title }}</p>
          <p class="text-xs text-stone-400 truncate mt-0.5">{{ p.summary }}</p>
          <div class="flex gap-1 mt-1 flex-wrap">
            <span v-for="kt in p.knowledgeTypeSlugs" :key="kt"
              class="bg-stone-100 text-stone-500 text-xs px-1.5 py-0.5 rounded">{{ kt }}</span>
          </div>
        </RouterLink>
        <div v-if="!loading && !pages.length" class="p-4 text-sm text-stone-400">No pages found.</div>
      </div>
    </aside>
    <!-- Right: router outlet for page detail -->
    <div class="flex-1 overflow-y-auto">
      <RouterView />
      <div v-if="!$route.params.slug" class="flex flex-col items-center justify-center h-full text-stone-400">
        <BookOpen :size="48" class="mb-4 opacity-30" />
        <p class="text-lg font-serif">Select a page to read</p>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { RouterLink, RouterView } from 'vue-router'
import { BookOpen } from 'lucide-vue-next'
import { wikiApi, type WikiPageSummary } from '@/api/wiki'

const pages = ref<WikiPageSummary[]>([])
const loading = ref(false)
const searchQ = ref('')
let searchTimer: ReturnType<typeof setTimeout> | null = null

async function load(q?: string) {
  loading.value = true
  try {
    const res = await wikiApi.list(undefined, q)
    pages.value = res.data
  } finally {
    loading.value = false
  }
}

function onSearch() {
  if (searchTimer) clearTimeout(searchTimer)
  searchTimer = setTimeout(() => load(searchQ.value || undefined), 350)
}

onMounted(() => load())
</script>
