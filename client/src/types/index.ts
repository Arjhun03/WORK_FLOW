export interface ApiResponse<T = any> {
  success: boolean;
  message: string;
  data: T;
  errors: string[];
}

export interface User {
  id: string;
  name: string;
  email: string;
  roles: string[];
  department: string;
  managerId?: string;
  managerName?: string;
  isActive: boolean;
  createdAt: string;
}

export interface Role {
  id: string;
  name: string;
  description: string;
}

export type ApproverType = 'User' | 'Role' | 'Manager' | 'DepartmentRole';

export interface WorkflowStep {
  stepId?: string;
  name: string;
  order: number;
  approverType: ApproverType;
  approverRole?: string;
  approverUserId?: string;
  approverDepartment?: string;
  isRequired: boolean;
}

export interface Workflow {
  id: string;
  name: string;
  description: string;
  requestType: string;
  version: number;
  isActive: boolean;
  createdBy: string;
  steps: WorkflowStep[];
  createdAt: string;
  updatedAt?: string;
}

export type RequestStatus = 
  | 'Draft' 
  | 'Pending' 
  | 'InProgress' 
  | 'Approved' 
  | 'Rejected' 
  | 'Cancelled' 
  | 'Completed';

export interface RequestItem {
  id: string;
  workflowId: string;
  workflowName: string;
  workflowVersion: number;
  requestedBy: string;
  requesterName: string;
  requesterDepartment: string;
  title: string;
  description: string;
  requestType: string;
  data: Record<string, any>;
  status: RequestStatus;
  currentStepOrder: number;
  currentApproverId?: string;
  currentApproverName?: string;
  currentApproverRole?: string;
  createdAt: string;
  updatedAt?: string;
  completedAt?: string;
}

export interface RequestDetail extends RequestItem {
  approvals: Approval[];
  comments: Comment[];
  auditLogs: AuditLogItem[];
  workflowSteps: WorkflowStep[];
}

export type ApprovalStatus = 'Pending' | 'Approved' | 'Rejected' | 'Skipped';

export interface Approval {
  id: string;
  requestId: string;
  requestTitle?: string;
  requesterName?: string;
  workflowStepId: string;
  stepName: string;
  stepOrder: number;
  approverId?: string;
  approverName?: string;
  approverRole?: string;
  status: ApprovalStatus;
  comments?: string;
  actionDate?: string;
  createdAt: string;
}

export interface Comment {
  id: string;
  requestId: string;
  userId: string;
  userName: string;
  message: string;
  createdAt: string;
}

export interface NotificationItem {
  id: string;
  userId: string;
  requestId: string;
  type: string;
  message: string;
  isRead: boolean;
  createdAt: string;
}

export interface AuditLogItem {
  id: string;
  userId: string;
  userName: string;
  requestId?: string;
  action: string;
  oldValue?: string;
  newValue?: string;
  metadata: Record<string, any>;
  timestamp: string;
}

export interface DashboardStats {
  totalUsers: number;
  activeWorkflows: number;
  totalRequests: number;
  pendingRequests: number;
  approvedRequests: number;
  rejectedRequests: number;
  completedRequests: number;
  myRequestsCount: number;
  pendingApprovalsCount: number;
  requestsByStatus: { name: string; value: number; color?: string }[];
  requestsByWorkflow: { name: string; value: number }[];
  requestsOverTime: { date: string; count: number }[];
}
