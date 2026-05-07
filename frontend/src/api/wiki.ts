import api from './axios'

export interface WikiPageSummary {
  id: string; slug: string; title: string; summary: string
  knowledgeTypeSlugs: string[]; sourceCount: number; updatedAt: string
  projectId?: string
}
export interface WikiPage extends WikiPageSummary {
  content: string; sourceIds: string[]; createdAt: string
}
export interface WikiSearchResult { slug: string; title: string; excerpt: string; score: number }

export const wikiApi = {
  list: (projectId?: string, q?: string) =>
    api.get<WikiPageSummary[]>('/wiki', { params: { projectId, q } }),
  get: (slug: string, projectId?: string) =>
    api.get<WikiPage>(`/wiki/${slug}`, { params: { projectId } }),
  create: (data: object) => api.post<WikiPage>('/wiki', data),
  update: (slug: string, data: object) => api.put<WikiPage>(`/wiki/${slug}`, data),
  delete: (slug: string) => api.delete(`/wiki/${slug}`),
  search: (q: string, projectId?: string, mode: 'fulltext' | 'semantic' = 'fulltext') =>
    api.get<WikiSearchResult[]>('/wiki/search', { params: { q, projectId, mode } }),
  revisions: (slug: string) => api.get<object[]>(`/wiki/${slug}/revisions`),
}
