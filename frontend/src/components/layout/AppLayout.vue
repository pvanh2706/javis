<template>
  <div class="flex h-screen overflow-hidden bg-linen">
    <!-- Sidebar -->
    <aside class="w-56 flex flex-col bg-stone-900 text-stone-100 shrink-0">
      <div class="px-5 py-5 border-b border-stone-700">
        <h1 class="font-serif text-2xl text-sienna font-bold tracking-wide">Javis</h1>
        <p class="text-xs text-stone-400 mt-0.5">Knowledge Base</p>
      </div>
      <nav class="flex-1 py-4 overflow-y-auto">
        <NavGroup label="Knowledge">
          <NavItem to="/wiki" icon="BookOpen">Wiki</NavItem>
          <NavItem to="/sources" icon="FileText">Sources</NavItem>
          <NavItem to="/drafts" icon="FilePen">Drafts</NavItem>
          <NavItem to="/projects" icon="FolderKanban">Projects</NavItem>
        </NavGroup>
        <NavGroup v-if="auth.isAdmin" label="Admin">
          <NavItem to="/admin/employees" icon="Users">Employees</NavItem>
          <NavItem to="/admin/departments" icon="Building2">Departments</NavItem>
          <NavItem to="/admin/roles" icon="Shield">Roles</NavItem>
          <NavItem to="/admin/knowledge-types" icon="Tag">Knowledge Types</NavItem>
          <NavItem to="/admin/settings" icon="Settings">AI Settings</NavItem>
          <NavItem to="/admin/audit" icon="ScrollText">Audit Log</NavItem>
        </NavGroup>
      </nav>
      <div class="p-4 border-t border-stone-700 flex items-center gap-3">
        <div class="w-8 h-8 rounded-full bg-sienna flex items-center justify-center text-white text-sm font-bold">
          {{ auth.employee?.name?.[0]?.toUpperCase() }}
        </div>
        <div class="flex-1 min-w-0">
          <p class="text-sm font-medium truncate">{{ auth.employee?.name }}</p>
          <RouterLink to="/profile" class="text-xs text-stone-400 hover:text-sienna">Profile</RouterLink>
        </div>
        <button @click="logout" class="text-stone-400 hover:text-red-400" title="Logout">
          <LogOut :size="16" />
        </button>
      </div>
    </aside>
    <!-- Main content -->
    <main class="flex-1 overflow-y-auto">
      <RouterView />
    </main>
  </div>
</template>

<script setup lang="ts">
import { RouterView, RouterLink, useRouter } from 'vue-router'
import { LogOut } from 'lucide-vue-next'
import { useAuthStore } from '@/stores/auth'
import NavGroup from '@/components/layout/NavGroup.vue'
import NavItem from '@/components/layout/NavItem.vue'

const auth = useAuthStore()
const router = useRouter()

function logout() {
  auth.logout()
  router.push('/login')
}
</script>
