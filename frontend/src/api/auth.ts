import api from './axios'

export interface LoginRequest { email: string; password: string }
export interface EmployeeDto {
  id: string; name: string; email: string; role: string
  departmentName?: string; isActive: boolean; mcpToken?: string; lastConnected?: string
}

export const authApi = {
  login: (data: LoginRequest) => api.post<{ token: string; employee: EmployeeDto }>('/auth/login', data),
  me: () => api.get<EmployeeDto>('/auth/me'),
  changePassword: (data: { currentPassword: string; newPassword: string }) =>
    api.put('/auth/change-password', data),
}
