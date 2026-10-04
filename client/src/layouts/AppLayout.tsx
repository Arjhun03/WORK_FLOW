import React, { useState } from 'react';
import { Outlet, NavLink, useLocation } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import { 
  LayoutDashboard, 
  FileText, 
  CheckSquare, 
  GitFork, 
  Users, 
  Bell, 
  History, 
  LogOut, 
  Menu, 
  X, 
  Sparkles,
  Database
} from 'lucide-react';
import { useAuth } from '../context/AuthContext';
import { approvalService, notificationService, authService } from '../services';

export const AppLayout: React.FC = () => {
  const { user, logout, isAdmin, isApprover } = useAuth();
  const [mobileMenuOpen, setMobileMenuOpen] = useState(false);
  const [seeding, setSeeding] = useState(false);
  const [seedSuccess, setSeedSuccess] = useState(false);
  const location = useLocation();

  // Pending Approvals badge count
  const { data: pendingApprovals = [] } = useQuery({
    queryKey: ['pendingApprovalsCount'],
    queryFn: () => approvalService.getPending(),
    refetchInterval: 15000,
    enabled: isApprover
  });

  // Unread Notifications badge count
  const { data: notifications = [] } = useQuery({
    queryKey: ['notificationsCount'],
    queryFn: () => notificationService.getNotifications(true),
    refetchInterval: 15000
  });

  const handleSeedDemo = async () => {
    try {
      setSeeding(true);
      await authService.seedData();
      setSeedSuccess(true);
      setTimeout(() => setSeedSuccess(false), 4000);
    } catch (err) {
      console.error('Seeding error', err);
    } finally {
      setSeeding(false);
    }
  };

  const navItems = [
    { name: 'Dashboard', path: '/dashboard', icon: LayoutDashboard },
    { name: 'Requests', path: '/requests', icon: FileText },
    ...(isApprover ? [{ 
      name: 'Approvals', 
      path: '/approvals', 
      icon: CheckSquare, 
      badge: pendingApprovals.length > 0 ? pendingApprovals.length : undefined 
    }] : []),
    ...(isAdmin ? [{ name: 'Workflows', path: '/workflows', icon: GitFork }] : []),
    ...(isAdmin ? [{ name: 'Users', path: '/users', icon: Users }] : []),
    { 
      name: 'Notifications', 
      path: '/notifications', 
      icon: Bell, 
      badge: notifications.length > 0 ? notifications.length : undefined 
    },
    ...(isAdmin ? [{ name: 'Audit Logs', path: '/audit-logs', icon: History }] : []),
  ];

  return (
    <div className="min-h-screen bg-[#0b0f19] flex text-slate-200">
      {/* Sidebar for Desktop */}
      <aside className="hidden lg:flex lg:flex-col w-64 glass-panel border-r border-slate-800/80 p-5 shrink-0 justify-between">
        <div>
          {/* Logo / Brand */}
          <div className="flex items-center gap-3 px-2 py-3 mb-6">
            <div className="w-10 h-10 rounded-xl bg-gradient-to-tr from-indigo-600 via-indigo-500 to-purple-500 flex items-center justify-center shadow-lg shadow-indigo-500/20 text-white font-bold">
              <Sparkles className="w-5 h-5" />
            </div>
            <div>
              <h1 className="text-base font-bold text-white tracking-tight">SmartWorkflow</h1>
              <p className="text-[11px] text-indigo-400 font-medium tracking-wide">Enterprise Approval Engine</p>
            </div>
          </div>

          {/* Navigation Links */}
          <nav className="space-y-1">
            {navItems.map((item) => {
              const Icon = item.icon;
              const isActive = location.pathname.startsWith(item.path);
              return (
                <NavLink
                  key={item.name}
                  to={item.path}
                  className={`flex items-center justify-between px-3.5 py-2.5 rounded-xl text-sm font-medium transition-all ${
                    isActive
                      ? 'bg-indigo-600/20 text-indigo-300 border border-indigo-500/30 shadow-sm'
                      : 'text-slate-400 hover:text-slate-200 hover:bg-slate-800/40'
                  }`}
                >
                  <div className="flex items-center gap-3">
                    <Icon className={`w-4 h-4 ${isActive ? 'text-indigo-400' : 'text-slate-400'}`} />
                    <span>{item.name}</span>
                  </div>
                  {item.badge !== undefined && (
                    <span className="px-2 py-0.5 text-xs font-bold rounded-full bg-indigo-500 text-white shadow-sm animate-pulse">
                      {item.badge}
                    </span>
                  )}
                </NavLink>
              );
            })}
          </nav>
        </div>

        {/* User Card & Logout */}
        <div className="space-y-3 pt-4 border-t border-slate-800/60">
          {isAdmin && (
            <button
              onClick={handleSeedDemo}
              disabled={seeding}
              className="w-full flex items-center justify-center gap-2 px-3 py-2 text-xs font-semibold text-purple-300 bg-purple-500/10 hover:bg-purple-500/20 border border-purple-500/20 rounded-xl transition-all"
            >
              <Database className="w-3.5 h-3.5" />
              <span>{seeding ? 'Seeding...' : seedSuccess ? 'Database Seeded!' : 'Seed Sample Workflows'}</span>
            </button>
          )}

          <div className="p-3 bg-slate-900/60 rounded-xl border border-slate-800/80 flex items-center justify-between">
            <div className="min-w-0 pr-2">
              <p className="text-xs font-semibold text-white truncate">{user?.name}</p>
              <p className="text-[11px] text-slate-400 truncate">{user?.department} • {user?.roles[0]}</p>
            </div>
            <button
              onClick={logout}
              title="Sign out"
              className="p-1.5 text-slate-400 hover:text-rose-400 hover:bg-rose-500/10 rounded-lg transition-colors"
            >
              <LogOut className="w-4 h-4" />
            </button>
          </div>
        </div>
      </aside>

      {/* Main Content Area */}
      <div className="flex-1 flex flex-col min-w-0">
        {/* Mobile Header */}
        <header className="lg:hidden flex items-center justify-between p-4 glass-panel border-b border-slate-800">
          <div className="flex items-center gap-2.5">
            <div className="w-8 h-8 rounded-lg bg-indigo-600 flex items-center justify-center text-white">
              <Sparkles className="w-4 h-4" />
            </div>
            <span className="font-bold text-white text-sm">SmartWorkflow</span>
          </div>
          <button
            onClick={() => setMobileMenuOpen(!mobileMenuOpen)}
            className="p-2 text-slate-400 hover:text-white rounded-lg"
          >
            {mobileMenuOpen ? <X className="w-5 h-5" /> : <Menu className="w-5 h-5" />}
          </button>
        </header>

        {/* Mobile Navigation Drawer */}
        {mobileMenuOpen && (
          <div className="lg:hidden glass-panel border-b border-slate-800 p-4 space-y-1">
            {navItems.map((item) => {
              const Icon = item.icon;
              return (
                <NavLink
                  key={item.name}
                  to={item.path}
                  onClick={() => setMobileMenuOpen(false)}
                  className="flex items-center justify-between px-3 py-2 rounded-lg text-sm font-medium text-slate-300 hover:bg-slate-800/50"
                >
                  <div className="flex items-center gap-3">
                    <Icon className="w-4 h-4 text-indigo-400" />
                    <span>{item.name}</span>
                  </div>
                  {item.badge !== undefined && (
                    <span className="px-2 py-0.5 text-xs font-bold rounded-full bg-indigo-500 text-white">
                      {item.badge}
                    </span>
                  )}
                </NavLink>
              );
            })}
            <div className="pt-3 border-t border-slate-800 flex justify-between items-center">
              <span className="text-xs text-slate-400">{user?.name} ({user?.roles[0]})</span>
              <button onClick={logout} className="text-xs text-rose-400 font-medium">Log out</button>
            </div>
          </div>
        )}

        {/* Page Content */}
        <main className="flex-1 p-4 md:p-8 overflow-y-auto">
          <Outlet />
        </main>
      </div>
    </div>
  );
};
