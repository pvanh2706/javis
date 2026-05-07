<template>
  <div class="p-8 max-w-3xl mx-auto">
    <h1 class="font-serif text-3xl text-stone-800 mb-6">AI Settings</h1>
    <div class="space-y-4">
      <div v-for="s in settings" :key="(s as any).key"
        class="bg-white rounded-xl border border-stone-200 p-4 flex items-center gap-4">
        <div class="flex-1">
          <p class="font-medium text-stone-800 text-sm">{{ (s as any).key }}</p>
          <input :type="(s as any).key.includes('key') || (s as any).key.includes('secret') ? 'password' : 'text'"
            v-model="editValues[(s as any).key]"
            class="mt-1 w-full border border-stone-200 rounded-lg px-3 py-1.5 text-sm font-mono" />
        </div>
        <button @click="save((s as any).key)"
          class="bg-sienna text-white rounded-lg px-4 py-2 text-xs hover:bg-sienna-dark shrink-0">Save</button>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted, reactive } from 'vue'
import { adminApi } from '@/api/index'

const settings = ref<object[]>([])
const editValues = reactive<Record<string, string>>({})

async function load() {
  settings.value = (await adminApi.getSettings()).data
  for (const s of settings.value as any[]) {
    editValues[s.key] = s.value ?? ''
  }
}

async function save(key: string) {
  await adminApi.updateSetting(key, editValues[key])
}

onMounted(load)
</script>
