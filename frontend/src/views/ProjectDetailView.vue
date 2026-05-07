<template>
  <div class="p-8 max-w-4xl mx-auto">
    <div v-if="project">
      <div class="flex items-center gap-3 mb-6">
        <RouterLink to="/projects" class="text-stone-400 hover:text-sienna"><ArrowLeft :size="18" /></RouterLink>
        <h1 class="font-serif text-3xl text-stone-800">{{ (project as any).name }}</h1>
      </div>
      <p class="text-stone-500 mb-6">{{ (project as any).description }}</p>
      <h2 class="font-serif text-xl text-stone-700 mb-3">Members</h2>
      <div class="bg-white rounded-xl border border-stone-200 overflow-hidden">
        <table class="w-full text-sm">
          <thead class="bg-stone-50 text-stone-500 text-xs uppercase">
            <tr>
              <th class="px-4 py-3 text-left">Name</th>
              <th class="px-4 py-3 text-left">Role</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="m in (project as any).members" :key="m.employeeId"
              class="border-t border-stone-100">
              <td class="px-4 py-3">{{ m.employeeName }}</td>
              <td class="px-4 py-3 text-stone-500 capitalize">{{ m.role }}</td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { RouterLink, useRoute } from 'vue-router'
import { ArrowLeft } from 'lucide-vue-next'
import { projectsApi } from '@/api/index'

const route = useRoute()
const project = ref<object | null>(null)

onMounted(async () => {
  project.value = (await projectsApi.get(route.params.id as string)).data
})
</script>
