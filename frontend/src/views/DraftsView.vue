<template>
  <div class="p-8 max-w-4xl mx-auto">
    <h1 class="font-serif text-3xl text-stone-800 mb-6">Wiki Drafts</h1>
    <div v-if="!drafts.length" class="text-stone-400 text-center py-12">No pending drafts.</div>
    <div v-for="d in drafts" :key="(d as any).id"
      class="bg-white rounded-xl border border-stone-200 p-6 mb-4">
      <div class="flex items-start justify-between">
        <div>
          <p class="font-medium text-stone-800">{{ (d as any).title }}</p>
          <p class="text-sm text-stone-400 mt-1">Proposed by {{ (d as any).proposedByName }}
            · {{ new Date((d as any).createdAt).toLocaleDateString() }}</p>
          <p class="text-sm text-stone-600 mt-2">{{ (d as any).summary }}</p>
        </div>
        <div v-if="auth.isAdmin" class="flex gap-2 shrink-0 ml-4">
          <button @click="approve((d as any).id)"
            class="bg-green-100 text-green-700 px-3 py-1.5 rounded-lg text-xs font-medium hover:bg-green-200">Approve</button>
          <button @click="reject((d as any).id)"
            class="bg-red-100 text-red-600 px-3 py-1.5 rounded-lg text-xs font-medium hover:bg-red-200">Reject</button>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { draftsApi } from '@/api/index'
import { useAuthStore } from '@/stores/auth'

const drafts = ref<object[]>([])
const auth = useAuthStore()

async function load() { drafts.value = (await draftsApi.list()).data }
async function approve(id: string) { await draftsApi.approve(id); await load() }
async function reject(id: string) { await draftsApi.reject(id); await load() }

onMounted(load)
</script>
