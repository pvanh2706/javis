<template>
  <div class="p-8 max-w-5xl mx-auto">
    <div class="flex items-center justify-between mb-6">
      <h1 class="font-serif text-3xl text-stone-800">Sources</h1>
      <button @click="showUpload = true"
        class="flex items-center gap-2 bg-sienna text-white px-4 py-2 rounded-lg text-sm hover:bg-sienna-dark">
        <Plus :size="16" /> Upload
      </button>
    </div>
    <div class="bg-white rounded-xl border border-stone-200 overflow-hidden">
      <table class="w-full text-sm">
        <thead class="bg-stone-50 text-stone-500 text-xs uppercase">
          <tr>
            <th class="px-4 py-3 text-left">Name</th>
            <th class="px-4 py-3 text-left">Type</th>
            <th class="px-4 py-3 text-left">Status</th>
            <th class="px-4 py-3 text-left">Uploaded</th>
            <th class="px-4 py-3"></th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="s in sources" :key="(s as any).id" class="border-t border-stone-100 hover:bg-linen/40">
            <td class="px-4 py-3 font-medium text-stone-800">{{ (s as any).name }}</td>
            <td class="px-4 py-3 text-stone-500">{{ (s as any).sourceType }}</td>
            <td class="px-4 py-3">
              <span :class="statusClass((s as any).status)"
                class="px-2 py-0.5 rounded-full text-xs font-medium">{{ (s as any).status }}</span>
            </td>
            <td class="px-4 py-3 text-stone-400">{{ new Date((s as any).createdAt).toLocaleDateString() }}</td>
            <td class="px-4 py-3 flex gap-2 justify-end">
              <button @click="recompile((s as any).id)" title="Recompile" class="text-stone-400 hover:text-sienna">
                <RefreshCw :size="15" />
              </button>
              <button @click="deleteSrc((s as any).id)" title="Delete" class="text-stone-400 hover:text-red-500">
                <Trash2 :size="15" />
              </button>
            </td>
          </tr>
        </tbody>
      </table>
    </div>

    <!-- Upload modal -->
    <div v-if="showUpload" class="fixed inset-0 bg-black/40 flex items-center justify-center z-50">
      <div class="bg-white rounded-2xl p-8 w-full max-w-md shadow-2xl">
        <h2 class="font-serif text-2xl mb-4">Upload Source</h2>
        <div class="space-y-3">
          <label class="block">
            <span class="text-sm text-stone-600">File</span>
            <input type="file" @change="onFile" class="mt-1 block w-full text-sm" />
          </label>
          <label class="block">
            <span class="text-sm text-stone-600">Or URL</span>
            <input v-model="urlInput" type="url" placeholder="https://…"
              class="mt-1 w-full border border-stone-200 rounded-lg px-3 py-2 text-sm" />
          </label>
        </div>
        <div class="flex gap-3 mt-6">
          <button @click="doUpload" class="flex-1 bg-sienna text-white rounded-lg py-2 text-sm hover:bg-sienna-dark">Upload</button>
          <button @click="showUpload = false" class="flex-1 border border-stone-200 rounded-lg py-2 text-sm">Cancel</button>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { Plus, RefreshCw, Trash2 } from 'lucide-vue-next'
import { sourcesApi } from '@/api/index'

const sources = ref<object[]>([])
const showUpload = ref(false)
const urlInput = ref('')
const fileInput = ref<File | null>(null)

function statusClass(s: string) {
  return { pending: 'bg-yellow-100 text-yellow-700', ready: 'bg-green-100 text-green-700',
           error: 'bg-red-100 text-red-600' }[s] ?? 'bg-stone-100 text-stone-500'
}

function onFile(e: Event) {
  fileInput.value = (e.target as HTMLInputElement).files?.[0] ?? null
}

async function load() { sources.value = (await sourcesApi.list()).data }

async function doUpload() {
  if (fileInput.value) await sourcesApi.upload(fileInput.value)
  else if (urlInput.value) await sourcesApi.uploadUrl({ url: urlInput.value })
  showUpload.value = false
  await load()
}

async function recompile(id: string) { await sourcesApi.recompile(id) }
async function deleteSrc(id: string) { await sourcesApi.delete(id); await load() }

onMounted(load)
</script>
