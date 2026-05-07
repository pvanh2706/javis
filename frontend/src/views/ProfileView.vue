<template>
  <div class="p-8 max-w-2xl mx-auto">
    <h1 class="font-serif text-3xl text-stone-800 mb-6">Profile</h1>
    <div v-if="auth.employee" class="bg-white rounded-xl border border-stone-200 p-6">
      <div class="flex items-center gap-4 mb-6">
        <div class="w-14 h-14 rounded-full bg-sienna flex items-center justify-center text-white text-2xl font-serif">
          {{ auth.employee.name[0].toUpperCase() }}
        </div>
        <div>
          <p class="font-medium text-stone-800 text-lg">{{ auth.employee.name }}</p>
          <p class="text-stone-400 text-sm">{{ auth.employee.email }}</p>
          <span class="text-xs bg-stone-100 text-stone-500 px-2 py-0.5 rounded-full capitalize">{{ auth.employee.role }}</span>
        </div>
      </div>

      <h2 class="font-medium text-stone-700 mb-3">MCP Token (for Claude Desktop)</h2>
      <div class="bg-stone-50 rounded-lg p-3 font-mono text-xs text-stone-600 break-all">
        {{ auth.employee.mcpToken ?? 'No token generated' }}
      </div>

      <h2 class="font-medium text-stone-700 mt-6 mb-3">Change Password</h2>
      <form @submit.prevent="changePassword" class="space-y-3">
        <input v-model="curr" type="password" placeholder="Current password"
          class="w-full border border-stone-200 rounded-lg px-3 py-2 text-sm" />
        <input v-model="next" type="password" placeholder="New password"
          class="w-full border border-stone-200 rounded-lg px-3 py-2 text-sm" />
        <button type="submit" class="bg-sienna text-white rounded-lg px-4 py-2 text-sm hover:bg-sienna-dark">Update</button>
        <p v-if="msg" class="text-sm" :class="msg.startsWith('Error') ? 'text-red-500' : 'text-green-600'">{{ msg }}</p>
      </form>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue'
import { useAuthStore } from '@/stores/auth'
import { authApi } from '@/api/auth'

const auth = useAuthStore()
const curr = ref(''); const next = ref(''); const msg = ref('')

async function changePassword() {
  try {
    await authApi.changePassword({ currentPassword: curr.value, newPassword: next.value })
    msg.value = 'Password updated!'
    curr.value = ''; next.value = ''
  } catch {
    msg.value = 'Error updating password'
  }
}
</script>
