import { api } from './api';
import type { 
  ApiResponse, 
  User, 
  Workflow, 
  WorkflowStep, 
  RequestItem, 
  RequestDetail, 
  Approval, 
  Comment, 
  NotificationItem, 
  AuditLogItem, 
  DashboardStats 
} from '../types';

export const authService = {
  login: async (email: string, password: string) => {
    const res = await api.post<ApiResponse<{ token: string; user: User }>>('/auth/login', { email, password });
    return res.data.data;
  },
  getMe: async () => {
    const res = await api.get<ApiResponse<User>>('/auth/me');
    return res.data.data;
  },
  seedData: async () => {
    const res = await api.post<ApiResponse>('/auth/seed');
    return res.data;
  }
};

export const userService = {
  getUsers: async (search?: string, department?: string) => {
    const res = await api.get<ApiResponse<User[]>>('/users', { params: { search, department } });
    return res.data.data;
  },
  getUserById: async (id: string) => {
    const res = await api.get<ApiResponse<User>>(`/users/${id}`);
    return res.data.data;
  },
  createUser: async (user: any) => {
    const res = await api.post<ApiResponse<User>>('/users', user);
    return res.data.data;
  },
  updateUser: async (id: string, user: any) => {
    const res = await api.put<ApiResponse<User>>(`/users/${id}`, user);
    return res.data.data;
  },
  deleteUser: async (id: string) => {
    const res = await api.delete<ApiResponse>(`/users/${id}`);
    return res.data;
  },
  getRoles: async () => {
    const res = await api.get<ApiResponse<any[]>>('/users/roles');
    return res.data.data;
  }
};

export const workflowService = {
  getWorkflows: async (activeOnly?: boolean) => {
    const res = await api.get<ApiResponse<Workflow[]>>('/workflows', { params: { activeOnly } });
    return res.data.data;
  },
  getWorkflowById: async (id: string) => {
    const res = await api.get<ApiResponse<Workflow>>(`/workflows/${id}`);
    return res.data.data;
  },
  createWorkflow: async (workflow: { name: string; description: string; requestType: string; steps: WorkflowStep[] }) => {
    const res = await api.post<ApiResponse<Workflow>>('/workflows', workflow);
    return res.data.data;
  },
  updateWorkflow: async (id: string, workflow: { name: string; description: string; requestType: string; isActive: boolean; steps: WorkflowStep[] }) => {
    const res = await api.put<ApiResponse<Workflow>>(`/workflows/${id}`, workflow);
    return res.data.data;
  },
  deleteWorkflow: async (id: string) => {
    const res = await api.delete<ApiResponse>(`/workflows/${id}`);
    return res.data;
  },
  activate: async (id: string) => {
    const res = await api.post<ApiResponse<Workflow>>(`/workflows/${id}/activate`);
    return res.data.data;
  },
  deactivate: async (id: string) => {
    const res = await api.post<ApiResponse<Workflow>>(`/workflows/${id}/deactivate`);
    return res.data.data;
  }
};

export const requestService = {
  getRequests: async (params?: { 
    status?: string; 
    workflowId?: string; 
    requestedBy?: string; 
    search?: string; 
    page?: number; 
    pageSize?: number;
    myRequestsOnly?: boolean;
  }) => {
    const res = await api.get<ApiResponse<{ items: RequestItem[]; totalCount: number; page: number; totalPages: number }>>('/requests', { params });
    return res.data.data;
  },
  getRequestById: async (id: string) => {
    const res = await api.get<ApiResponse<RequestDetail>>(`/requests/${id}`);
    return res.data.data;
  },
  createRequest: async (data: { workflowId: string; title: string; description: string; data?: Record<string, any> }) => {
    const res = await api.post<ApiResponse<RequestItem>>('/requests', data);
    return res.data.data;
  },
  updateRequest: async (id: string, data: { title?: string; description?: string; data?: Record<string, any> }) => {
    const res = await api.put<ApiResponse<RequestItem>>(`/requests/${id}`, data);
    return res.data.data;
  },
  cancelRequest: async (id: string) => {
    const res = await api.post<ApiResponse<RequestItem>>(`/requests/${id}/cancel`);
    return res.data.data;
  },
  approveRequest: async (id: string, comments?: string) => {
    const res = await api.post<ApiResponse<RequestItem>>(`/requests/${id}/approve`, { comments });
    return res.data.data;
  },
  rejectRequest: async (id: string, comments?: string) => {
    const res = await api.post<ApiResponse<RequestItem>>(`/requests/${id}/reject`, { comments });
    return res.data.data;
  },
  getComments: async (requestId: string) => {
    const res = await api.get<ApiResponse<Comment[]>>(`/requests/${requestId}/comments`);
    return res.data.data;
  },
  addComment: async (requestId: string, message: string) => {
    const res = await api.post<ApiResponse<Comment>>(`/requests/${requestId}/comments`, { message });
    return res.data.data;
  }
};

export const approvalService = {
  getPending: async () => {
    const res = await api.get<ApiResponse<Approval[]>>('/approvals/pending');
    return res.data.data;
  }
};

export const notificationService = {
  getNotifications: async (unreadOnly = false) => {
    const res = await api.get<ApiResponse<NotificationItem[]>>('/notifications', { params: { unreadOnly } });
    return res.data.data;
  },
  markAsRead: async (id: string) => {
    const res = await api.put<ApiResponse>(`/notifications/${id}/read`);
    return res.data;
  },
  markAllAsRead: async () => {
    const res = await api.put<ApiResponse>('/notifications/read-all');
    return res.data;
  }
};

export const auditLogService = {
  getLogs: async (params?: { requestId?: string; page?: number; pageSize?: number }) => {
    const res = await api.get<ApiResponse<{ items: AuditLogItem[]; totalCount: number; page: number; totalPages: number }>>('/audit-logs', { params });
    return res.data.data;
  }
};

export const dashboardService = {
  getStats: async () => {
    const res = await api.get<ApiResponse<DashboardStats>>('/dashboard');
    return res.data.data;
  }
};
