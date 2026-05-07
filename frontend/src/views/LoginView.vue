<template>
  <div class="min-h-screen bg-linen flex items-center justify-center">
    <div class="bg-white rounded-2xl shadow-lg p-10 w-full max-w-sm">
      <h1 class="font-serif text-4xl text-stone-800 mb-1">Javis</h1>
      <p class="text-stone-400 text-sm mb-8">Knowledge Base Portal</p>

      <form @submit.prevent="handleLogin" class="space-y-4">
        <div>
          <label class="block text-sm font-medium text-stone-600 mb-1">Email</label>
          <input v-model="email" type="email" required
            class="w-full border border-stone-200 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-sienna/30 focus:border-sienna" />
        </div>
        <div>
          <label class="block text-sm font-medium text-stone-600 mb-1">Password</label>
          <input v-model="password" type="password" required
            class="w-full border border-stone-200 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-sienna/30 focus:border-sienna" />
        </div>
        <p v-if="error" class="text-red-500 text-sm">{{ error }}</p>
        <button type="submit" :disabled="loading"
          class="w-full bg-sienna text-white rounded-lg py-2 text-sm font-medium hover:bg-sienna-dark transition-colors disabled:opacity-50">
          {{ loading ? 'Signing in…' : 'Sign In' }}
        </button>
      </form>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth'

const email = ref('')
const password = ref('')
const error = ref('')
const loading = ref(false)
const auth = useAuthStore()
const router = useRouter()

async function handleLogin() {
  error.value = ''
  loading.value = true
  try {
    await auth.login(email.value, password.value)
    router.push('/')
  } catch {
    error.value = 'Invalid email or password'
  } finally {
    loading.value = false
  }
}
</script>
