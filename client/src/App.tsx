import React from 'react';
import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { AuthProvider } from './context/AuthContext';
import { ProtectedRoute } from './routes/ProtectedRoute';
import { AppLayout } from './layouts/AppLayout';

// Pages
import { LoginPage } from './pages/LoginPage';
import { DashboardPage } from './pages/DashboardPage';
import { RequestsPage } from './pages/RequestsPage';
import { CreateRequestPage } from './pages/CreateRequestPage';
import { RequestDetailPage } from './pages/RequestDetailPage';
import { ApprovalsPage } from './pages/ApprovalsPage';
import { WorkflowsPage } from './pages/WorkflowsPage';
import { WorkflowBuilderPage } from './pages/WorkflowBuilderPage';
import { UsersPage } from './pages/UsersPage';
import { NotificationsPage } from './pages/NotificationsPage';
import { AuditLogsPage } from './pages/AuditLogsPage';

const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      retry: 1,
      refetchOnWindowFocus: false,
      staleTime: 5000,
    },
  },
});

export const App: React.FC = () => {
  return (
    <QueryClientProvider client={queryClient}>
      <AuthProvider>
        <BrowserRouter>
          <Routes>
            {/* Public Auth Route */}
            <Route path="/login" element={<LoginPage />} />

            {/* Protected App Routes inside AppLayout */}
            <Route
              element={
                <ProtectedRoute>
                  <AppLayout />
                </ProtectedRoute>
              }
            >
              <Route path="/dashboard" element={<DashboardPage />} />
              <Route path="/requests" element={<RequestsPage />} />
              <Route path="/requests/create" element={<CreateRequestPage />} />
              <Route path="/requests/:id" element={<RequestDetailPage />} />
              
              {/* Approver-accessible Route */}
              <Route
                path="/approvals"
                element={
                  <ProtectedRoute approverOnly>
                    <ApprovalsPage />
                  </ProtectedRoute>
                }
              />

              {/* Admin-only Routes */}
              <Route
                path="/workflows"
                element={
                  <ProtectedRoute requiredRole="Admin">
                    <WorkflowsPage />
                  </ProtectedRoute>
                }
              />
              <Route
                path="/workflows/create"
                element={
                  <ProtectedRoute requiredRole="Admin">
                    <WorkflowBuilderPage />
                  </ProtectedRoute>
                }
              />
              <Route
                path="/workflows/:id/edit"
                element={
                  <ProtectedRoute requiredRole="Admin">
                    <WorkflowBuilderPage />
                  </ProtectedRoute>
                }
              />
              <Route
                path="/users"
                element={
                  <ProtectedRoute requiredRole="Admin">
                    <UsersPage />
                  </ProtectedRoute>
                }
              />
              <Route
                path="/audit-logs"
                element={
                  <ProtectedRoute requiredRole="Admin">
                    <AuditLogsPage />
                  </ProtectedRoute>
                }
              />

              <Route path="/notifications" element={<NotificationsPage />} />
            </Route>

            {/* Default Catch-all */}
            <Route path="*" element={<Navigate to="/dashboard" replace />} />
          </Routes>
        </BrowserRouter>
      </AuthProvider>
    </QueryClientProvider>
  );
};

export default App;
