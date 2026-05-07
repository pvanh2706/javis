<template>
  <div class="p-8 max-w-5xl mx-auto">
    <div class="flex items-center justify-between mb-6">
      <h1 class="font-serif text-3xl text-stone-800">Projects</h1>
      <button @click="showCreate = true"
        class="flex items-center gap-2 bg-sienna text-white px-4 py-2 rounded-lg text-sm hover:bg-sienna-dark">
        <Plus :size="16" /> New Project
      </button>
    </div>
    <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
      <RouterLink v-for="p in projects" :key="(p as any).id" :to="`/projects/${(p as any).id}`"
        class="bg-white rounded-xl border border-stone-200 p-5 hover:border-sienna/50 hover:shadow-sm transition-all">
        <div class="flex items-center gap-2 mb-2">
          <FolderKanban :size="18" class="text-sienna" />
          <span class="font-medium text-stone-800">{{ (p as any).name }}</span>
        </div>
        <p class="text-sm text-stone-500">{{ (p as any).description }}</p>
        <p class="text-xs text-stone-400 mt-3">{{ (p as any).memberCount }} members</p>
      </RouterLink>
    </div>

    <div v-if="showCreate" class="fixed inset-0 bg-black/40 flex items-center justify-center z-50">
      <div class="bg-white rounded-2xl p-8 w-full max-w-md">
        <h2 class="font-serif text-2xl mb-4">New Project</h2>
        <div class="space-y-3">
          <input v-model="form.name" placeholder="Name" class="w-full border border-stone-200 rounded-lg px-3 py-2 text-sm" />
          <textarea v-model="form.description" placeholder="Description" rows="3"
            class="w-full border border-stone-200 rounded-lg px-3 py-2 text-sm resize-none" />
        </div>
        <div class="flex gap-3 mt-5">
          <button @click="create" class="flex-1 bg-sienna text-white rounded-lg py-2 text-sm">Create</button>
          <button @click="showCreate = false" class="flex-1 border border-stone-200 rounded-lg py-2 text-sm">Cancel</button>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted, reactive } from 'vue'
import { RouterLink } from 'vue-router'
import { Plus, FolderKanban } from 'lucide-vue-next'
import { projectsApi } from '@/api/index'

const projects = ref<object[]>([])
const showCreate = ref(false)
const form = reactive({ name: '', description: '' })

async function load() { projects.value = (await projectsApi.list()).data }
async function create() {
  await projectsApi.create(form)
  showCreate.value = false
  form.name = ''; form.description = ''
  await load()
}

onMounted(load)
</script>
