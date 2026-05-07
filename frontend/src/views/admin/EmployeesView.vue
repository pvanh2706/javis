<template>
  <div class="p-8 max-w-5xl mx-auto">
    <div class="flex items-center justify-between mb-6">
      <h1 class="font-serif text-3xl text-stone-800">Employees</h1>
      <button @click="openCreate" class="flex items-center gap-2 bg-sienna text-white px-4 py-2 rounded-lg text-sm hover:bg-sienna-dark">
        <Plus :size="16" /> Add Employee
      </button>
    </div>
    <div class="bg-white rounded-xl border border-stone-200 overflow-hidden">
      <table class="w-full text-sm">
        <thead class="bg-stone-50 text-stone-500 text-xs uppercase">
          <tr>
            <th class="px-4 py-3 text-left">Name</th>
            <th class="px-4 py-3 text-left">Email</th>
            <th class="px-4 py-3 text-left">Dept</th>
            <th class="px-4 py-3 text-left">Role</th>
            <th class="px-4 py-3 text-left">Status</th>
            <th class="px-4 py-3"></th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="e in employees" :key="(e as any).id" class="border-t border-stone-100">
            <td class="px-4 py-3 font-medium">{{ (e as any).name }}</td>
            <td class="px-4 py-3 text-stone-500">{{ (e as any).email }}</td>
            <td class="px-4 py-3 text-stone-400">{{ (e as any).departmentName }}</td>
            <td class="px-4 py-3 text-stone-400 capitalize">{{ (e as any).role }}</td>
            <td class="px-4 py-3">
              <span :class="(e as any).isActive ? 'bg-green-100 text-green-700' : 'bg-stone-100 text-stone-400'"
                class="px-2 py-0.5 rounded-full text-xs">{{ (e as any).isActive ? 'Active' : 'Inactive' }}</span>
            </td>
            <td class="px-4 py-3 text-right">
              <button @click="deletePerson((e as any).id)" class="text-stone-300 hover:text-red-500"><Trash2 :size="14" /></button>
            </td>
          </tr>
        </tbody>
      </table>
    </div>

    <div v-if="showModal" class="fixed inset-0 bg-black/40 flex items-center justify-center z-50">
      <div class="bg-white rounded-2xl p-8 w-full max-w-md">
        <h2 class="font-serif text-2xl mb-4">New Employee</h2>
        <div class="space-y-3">
          <input v-model="form.name" placeholder="Full name" class="w-full border border-stone-200 rounded-lg px-3 py-2 text-sm" />
          <input v-model="form.email" type="email" placeholder="Email" class="w-full border border-stone-200 rounded-lg px-3 py-2 text-sm" />
          <input v-model="form.password" type="password" placeholder="Password" class="w-full border border-stone-200 rounded-lg px-3 py-2 text-sm" />
          <select v-model="form.role" class="w-full border border-stone-200 rounded-lg px-3 py-2 text-sm">
            <option value="employee">Employee</option>
            <option value="admin">Admin</option>
          </select>
        </div>
        <div class="flex gap-3 mt-5">
          <button @click="createEmployee" class="flex-1 bg-sienna text-white rounded-lg py-2 text-sm">Create</button>
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

const employees = ref<object[]>([])
const showModal = ref(false)
const form = reactive({ name: '', email: '', password: '', role: 'employee' })

function openCreate() { showModal.value = true }
async function load() { employees.value = (await rbacApi.getEmployees()).data }
async function createEmployee() {
  await rbacApi.createEmployee(form)
  showModal.value = false
  await load()
}
async function deletePerson(id: string) {
  if (confirm('Delete this employee?')) { await rbacApi.deleteEmployee(id); await load() }
}

onMounted(load)
</script>
