<template>
  <div class="p-8 max-w-5xl mx-auto">
    <h1 class="font-serif text-3xl text-stone-800 mb-6">Audit Log</h1>
    <div class="bg-white rounded-xl border border-stone-200 overflow-hidden">
      <table class="w-full text-sm">
        <thead class="bg-stone-50 text-stone-500 text-xs uppercase">
          <tr>
            <th class="px-4 py-3 text-left">Time</th>
            <th class="px-4 py-3 text-left">Actor</th>
            <th class="px-4 py-3 text-left">Action</th>
            <th class="px-4 py-3 text-left">Resource</th>
            <th class="px-4 py-3 text-left">Details</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="log in logs" :key="(log as any).id" class="border-t border-stone-100 text-xs">
            <td class="px-4 py-2 text-stone-400">{{ new Date((log as any).createdAt).toLocaleString() }}</td>
            <td class="px-4 py-2">{{ (log as any).actorName }}</td>
            <td class="px-4 py-2 font-mono text-sienna">{{ (log as any).action }}</td>
            <td class="px-4 py-2 text-stone-500">{{ (log as any).resourceType }} {{ (log as any).resourceId?.slice(0, 8) }}</td>
            <td class="px-4 py-2 text-stone-400 truncate max-w-xs">{{ (log as any).details }}</td>
          </tr>
        </tbody>
      </table>
    </div>
    <div class="flex gap-2 mt-4 justify-end">
      <button @click="prev" :disabled="page <= 1" class="px-3 py-1.5 text-sm border rounded-lg disabled:opacity-40">←</button>
      <span class="px-3 py-1.5 text-sm text-stone-500">Page {{ page }}</span>
      <button @click="next" class="px-3 py-1.5 text-sm border rounded-lg">→</button>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted, watch } from 'vue'
import { adminApi } from '@/api/index'

const logs = ref<object[]>([])
const page = ref(1)

async function load() {
  const res = (await adminApi.getAuditLog(page.value)).data as any
  logs.value = res.items ?? res
}

function prev() { if (page.value > 1) page.value-- }
function next() { page.value++ }

watch(page, load)
onMounted(load)
</script>
