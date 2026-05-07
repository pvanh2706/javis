import api from './axios'

export const sourcesApi = {
  list: (projectId?: string) => api.get<object[]>('/sources', { params: { projectId } }),
  upload: (file: File, knowledgeTypeId?: string, departmentIds?: string[], projectId?: string) => {
    const fd = new FormData()
    fd.append('file', file)
    if (knowledgeTypeId) fd.append('knowledgeTypeId', knowledgeTypeId)
    departmentIds?.forEach(id => fd.append('departmentIds', id))
    if (projectId) fd.append('projectId', projectId)
    return api.post('/sources/upload', fd, { headers: { 'Content-Type': 'multipart/form-data' } })
  },
  uploadUrl: (data: object) => api.post('/sources/upload-url', data),
  delete: (id: string) => api.delete(`/sources/${id}`),
  recompile: (id: string) => api.post(`/sources/${id}/recompile`),
}

export const draftsApi = {
  list: () => api.get<object[]>('/wiki-drafts'),
  propose: (data: object) => api.post('/wiki-drafts', data),
  approve: (id: string, comment?: string) => api.put(`/wiki-drafts/${id}/approve`, { comment }),
  reject: (id: string, comment?: string) => api.put(`/wiki-drafts/${id}/reject`, { comment }),
}

export const projectsApi = {
  list: () => api.get<object[]>('/projects'),
  get: (id: string) => api.get(`/projects/${id}`),
  create: (data: object) => api.post('/projects', data),
  update: (id: string, data: object) => api.put(`/projects/${id}`, data),
  delete: (id: string) => api.delete(`/projects/${id}`),
  addMember: (id: string, data: object) => api.post(`/projects/${id}/members`, data),
  updateMember: (id: string, empId: string, data: object) =>
    api.put(`/projects/${id}/members/${empId}`, data),
  removeMember: (id: string, empId: string) => api.delete(`/projects/${id}/members/${empId}`),
}

export const adminApi = {
  getSettings: () => api.get<object[]>('/admin/settings'),
  updateSetting: (key: string, value: string) => api.put(`/admin/settings/${key}`, { value }),
  getAuditLog: (page = 1, pageSize = 50) =>
    api.get('/admin/audit-log', { params: { page, pageSize } }),
}

export const rbacApi = {
  getDepartments: () => api.get<object[]>('/departments'),
  createDepartment: (data: object) => api.post('/departments', data),
  updateDepartment: (id: string, data: object) => api.put(`/departments/${id}`, data),
  deleteDepartment: (id: string) => api.delete(`/departments/${id}`),

  getEmployees: (q?: string) => api.get<object[]>('/employees', { params: { q } }),
  getEmployee: (id: string) => api.get(`/employees/${id}`),
  createEmployee: (data: object) => api.post('/employees', data),
  updateEmployee: (id: string, data: object) => api.put(`/employees/${id}`, data),
  deleteEmployee: (id: string) => api.delete(`/employees/${id}`),
  generateMcpToken: (id: string) => api.post(`/employees/${id}/mcp-token`),

  getRoles: () => api.get<object[]>('/roles'),
  createRole: (data: object) => api.post('/roles', data),
  updateRole: (id: string, data: object) => api.put(`/roles/${id}`, data),
  deleteRole: (id: string) => api.delete(`/roles/${id}`),

  getKnowledgeTypes: () => api.get<object[]>('/knowledge-types'),
  createKnowledgeType: (data: object) => api.post('/knowledge-types', data),
  updateKnowledgeType: (id: string, data: object) => api.put(`/knowledge-types/${id}`, data),
  deleteKnowledgeType: (id: string) => api.delete(`/knowledge-types/${id}`),
}
