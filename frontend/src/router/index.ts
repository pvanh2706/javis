import { createRouter, createWebHistory } from 'vue-router'
import { useAuthStore } from '@/stores/auth'

const router = createRouter({
  history: createWebHistory(),
  routes: [
    { path: '/login', component: () => import('@/views/LoginView.vue'), meta: { public: true } },
    {
      path: '/',
      component: () => import('@/components/layout/AppLayout.vue'),
      children: [
        { path: '', redirect: '/wiki' },
        { path: 'wiki', component: () => import('@/views/wiki/WikiBrowserView.vue') },
        { path: 'wiki/:slug', component: () => import('@/views/wiki/WikiPageView.vue') },
        { path: 'sources', component: () => import('@/views/SourcesView.vue') },
        { path: 'projects', component: () => import('@/views/ProjectsView.vue') },
        { path: 'projects/:id', component: () => import('@/views/ProjectDetailView.vue') },
        { path: 'drafts', component: () => import('@/views/DraftsView.vue') },
        {
          path: 'admin',
          meta: { adminOnly: true },
          children: [
            { path: 'employees', component: () => import('@/views/admin/EmployeesView.vue') },
            { path: 'departments', component: () => import('@/views/admin/DepartmentsView.vue') },
            { path: 'roles', component: () => import('@/views/admin/RolesView.vue') },
            { path: 'knowledge-types', component: () => import('@/views/admin/KnowledgeTypesView.vue') },
            { path: 'settings', component: () => import('@/views/admin/SettingsView.vue') },
            { path: 'audit', component: () => import('@/views/admin/AuditView.vue') },
          ]
        },
        { path: 'profile', component: () => import('@/views/ProfileView.vue') },
      ]
    },
    { path: '/:pathMatch(.*)*', redirect: '/' }
  ]
})

router.beforeEach(async (to) => {
  const auth = useAuthStore()
  if (!to.meta.public && !auth.isLoggedIn) return '/login'
  if (to.meta.public && auth.isLoggedIn) return '/'
  if (to.meta.adminOnly && !auth.isAdmin) return '/'
  if (auth.isLoggedIn && !auth.employee) await auth.fetchMe()
})

export default router
