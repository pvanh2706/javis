import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import { authApi, type EmployeeDto } from '@/api/auth'

export const useAuthStore = defineStore('auth', () => {
  const token = ref<string | null>(localStorage.getItem('token'))
  const employee = ref<EmployeeDto | null>(null)

  const isLoggedIn = computed(() => !!token.value)
  const isAdmin = computed(() => employee.value?.role === 'admin')

  async function login(email: string, password: string) {
    const res = await authApi.login({ email, password })
    token.value = res.data.token
    employee.value = res.data.employee
    localStorage.setItem('token', res.data.token)
  }

  async function fetchMe() {
    if (!token.value) return
    const res = await authApi.me()
    employee.value = res.data
  }

  function logout() {
    token.value = null
    employee.value = null
    localStorage.removeItem('token')
  }

  return { token, employee, isLoggedIn, isAdmin, login, fetchMe, logout }
})
