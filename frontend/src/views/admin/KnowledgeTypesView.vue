<template>
  <div class="p-8 max-w-3xl mx-auto">
    <div class="flex items-center justify-between mb-6">
      <h1 class="font-serif text-3xl text-stone-800">Knowledge Types</h1>
      <button @click="showModal = true" class="flex items-center gap-2 bg-sienna text-white px-4 py-2 rounded-lg text-sm hover:bg-sienna-dark"><Plus :size="16" /> Add</button>
    </div>
    <div class="space-y-2">
      <div v-for="kt in kts" :key="(kt as any).id" class="bg-white rounded-xl border border-stone-200 px-4 py-3 flex items-center justify-between">
        <div>
          <p class="font-medium text-stone-800">{{ (kt as any).name }}</p>
          <p class="text-xs text-stone-500 font-mono">{{ (kt as any).slug }}</p>
        </div>
        <button @click="del((kt as any).id)" class="text-stone-300 hover:text-red-500"><Trash2 :size="14" /></button>
      </div>
    </div>
    <div v-if="showModal" class="fixed inset-0 bg-black/40 flex items-center justify-center z-50">
      <div class="bg-white rounded-2xl p-8 w-full max-w-sm">
        <h2 class="font-serif text-xl mb-4">New Knowledge Type</h2>
        <input v-model="form.name" placeholder="Name (e.g. SOP)" class="w-full border border-stone-200 rounded-lg px-3 py-2 text-sm mb-3" />
        <input v-model="form.slug" placeholder="Slug (e.g. sop)" class="w-full border border-stone-200 rounded-lg px-3 py-2 text-sm" />
        <div class="flex gap-3 mt-4">
          <button @click="create" class="flex-1 bg-sienna text-white rounded-lg py-2 text-sm">Create</button>
          <button @click="showModal = false" class="flex-1 border border-stone-200 rounded-lg py-2 text-sm">Cancel</button>
        </div>
      </div>
    </div>
  </div>
</template>
<script setup lang="ts">
import { ref, onMounted, reactive } from 'vue'
import { Plus, Trash2 } from 'lucide-vue-next'
import { rbacApi } from '@/api/index'
const kts = ref<object[]>([])
const showModal = ref(false)
const form = reactive({ name: '', slug: '' })
async function load() { kts.value = (await rbacApi.getKnowledgeTypes()).data }
async function create() { await rbacApi.createKnowledgeType(form); showModal.value = false; await load() }
async function del(id: string) { await rbacApi.deleteKnowledgeType(id); await load() }
onMounted(load)
</script>
