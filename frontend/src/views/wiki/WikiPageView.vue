<template>
  <div class="max-w-4xl mx-auto p-8">
    <div v-if="loading" class="text-stone-400 py-12 text-center">Loading…</div>
    <div v-else-if="page">
      <div class="mb-6">
        <div class="flex gap-2 mb-2">
          <span v-for="kt in page.knowledgeTypeSlugs" :key="kt"
            class="bg-sienna/10 text-sienna text-xs px-2 py-0.5 rounded font-medium">{{ kt }}</span>
        </div>
        <h1 class="font-serif text-4xl text-stone-900 mb-2">{{ page.title }}</h1>
        <p class="text-stone-500 text-sm">Updated {{ new Date(page.updatedAt).toLocaleDateString() }}</p>
      </div>
      <article class="prose prose-stone max-w-none font-sans" v-html="renderedContent" />
    </div>
    <div v-else class="text-stone-400 py-12 text-center">Page not found.</div>
  </div>
</template>

<script setup lang="ts">
import { ref, watch, computed } from 'vue'
import { useRoute } from 'vue-router'
import MarkdownIt from 'markdown-it'
import { wikiApi, type WikiPage } from '@/api/wiki'

const route = useRoute()
const page = ref<WikiPage | null>(null)
const loading = ref(false)
const md = new MarkdownIt({ html: false, linkify: true })

const renderedContent = computed(() => page.value ? md.render(page.value.content) : '')

async function load(slug: string) {
  loading.value = true
  try {
    const res = await wikiApi.get(slug)
    page.value = res.data
  } catch {
    page.value = null
  } finally {
    loading.value = false
  }
}

watch(() => route.params.slug, (slug) => {
  if (slug) load(slug as string)
}, { immediate: true })
</script>
