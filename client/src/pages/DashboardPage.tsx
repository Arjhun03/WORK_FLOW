import React from 'react';
import { useQuery } from '@tanstack/react-query';
import { Link } from 'react-router-dom';
import { 
  FileText, 
  Clock, 
  CheckCircle2, 
  XCircle, 
  GitFork, 
  PlusCircle, 
  CheckSquare,
  TrendingUp,
  ArrowRight
} from 'lucide-react';
import { 
  ResponsiveContainer, 
  AreaChart, 
  Area, 
  XAxis, 
  YAxis, 
  Tooltip, 
  BarChart, 
  Bar, 
  PieChart, 
  Pie, 
  Cell 
} from 'recharts';
import { useAuth } from '../context/AuthContext';
import { dashboardService } from '../services';

export const DashboardPage: React.FC = () => {
  const { user, isApprover, isAdmin } = useAuth();

  const { data: stats, isLoading } = useQuery({
    queryKey: ['dashboardStats'],
    queryFn: () => dashboardService.getStats(),
    refetchInterval: 10000
  });

  const cards = [
    {
      title: 'Total Requests',
      value: stats?.totalRequests ?? 0,
      icon: FileText,
      color: 'from-blue-600 to-indigo-600',
      badge: 'All-time'
    },
    {
      title: 'Pending Action',
      value: stats?.pendingRequests ?? 0,
      icon: Clock,
      color: 'from-amber-500 to-orange-600',
      badge: 'Awaiting decision'
    },
    {
      title: 'Completed',
      value: stats?.completedRequests ?? 0,
      icon: CheckCircle2,
      color: 'from-emerald-500 to-teal-600',
      badge: 'Fully processed'
    },
    {
      title: 'Rejected',
      value: stats?.rejectedRequests ?? 0,
      icon: XCircle,
      color: 'from-rose-500 to-red-600',
      badge: 'Declined'
    },
    {
      title: 'Active Workflows',
      value: stats?.activeWorkflows ?? 0,
      icon: GitFork,
      color: 'from-purple-600 to-indigo-600',
      badge: 'Configured'
    }
  ];

  const statusColors = ['#f59e0b', '#3b82f6', '#10b981', '#ef4444', '#8b5cf6'];

  return (
    <div className="space-y-8 max-w-7xl mx-auto">
      {/* Welcome Banner */}
      <div className="glass-panel p-6 sm:p-8 rounded-3xl border border-slate-800 flex flex-col sm:flex-row sm:items-center justify-between gap-6 relative overflow-hidden">
        <div className="relative z-10 space-y-2">
          <div className="inline-flex items-center gap-2 px-3 py-1 rounded-full bg-indigo-500/10 border border-indigo-500/20 text-indigo-400 text-xs font-semibold">
            <span className="w-2 h-2 rounded-full bg-indigo-400 animate-pulse"></span>
            Role: {user?.roles.join(', ')} • {user?.department}
          </div>
          <h1 className="text-2xl sm:text-3xl font-bold text-white tracking-tight">
            Welcome back, {user?.name}
          </h1>
          <p className="text-slate-400 text-sm max-w-xl">
            Track and route approval requests through automated multi-stage organizational workflows.
          </p>
        </div>

        <div className="relative z-10 flex flex-wrap gap-3">
          <Link
            to="/requests/create"
            className="flex items-center gap-2 px-4 py-2.5 bg-gradient-to-r from-indigo-600 to-indigo-500 hover:from-indigo-500 hover:to-indigo-400 text-white rounded-xl text-sm font-semibold shadow-lg shadow-indigo-600/25 transition-all"
          >
            <PlusCircle className="w-4 h-4" />
            <span>Create Request</span>
          </Link>

          {isApprover && (
            <Link
              to="/approvals"
              className="flex items-center gap-2 px-4 py-2.5 glass-card hover:bg-slate-800 text-white rounded-xl text-sm font-semibold transition-all"
            >
              <CheckSquare className="w-4 h-4 text-amber-400" />
              <span>Pending Queue ({stats?.pendingApprovalsCount ?? 0})</span>
            </Link>
          )}

          {isAdmin && (
            <Link
              to="/workflows"
              className="flex items-center gap-2 px-4 py-2.5 glass-card hover:bg-slate-800 text-white rounded-xl text-sm font-semibold transition-all"
            >
              <GitFork className="w-4 h-4 text-purple-400" />
              <span>Workflow Builder</span>
            </Link>
          )}
        </div>
      </div>

      {/* Metric Cards Grid */}
      <div className="grid grid-cols-2 md:grid-cols-3 lg:grid-cols-5 gap-4">
        {cards.map((card) => {
          const Icon = card.icon;
          return (
            <div key={card.title} className="glass-card p-5 rounded-2xl flex flex-col justify-between">
              <div className="flex items-center justify-between mb-3">
                <span className="text-xs font-semibold text-slate-400">{card.title}</span>
                <div className={`w-8 h-8 rounded-lg bg-gradient-to-br ${card.color} flex items-center justify-center text-white shadow-md`}>
                  <Icon className="w-4 h-4" />
                </div>
              </div>
              <div>
                <p className="text-2xl font-black text-white">
                  {isLoading ? '...' : card.value}
                </p>
                <p className="text-[11px] text-slate-500 mt-1 font-medium">{card.badge}</p>
              </div>
            </div>
          );
        })}
      </div>

      {/* Analytics Charts */}
      <div className="grid lg:grid-cols-3 gap-6">
        {/* Trend Over Time (Area Chart) */}
        <div className="lg:col-span-2 glass-panel p-6 rounded-3xl border border-slate-800/80 space-y-4">
          <div className="flex items-center justify-between">
            <div>
              <h2 className="text-base font-bold text-white flex items-center gap-2">
                <TrendingUp className="w-4 h-4 text-indigo-400" />
                <span>Requests Activity (Last 7 Days)</span>
              </h2>
              <p className="text-xs text-slate-400">Total volume of requests submitted</p>
            </div>
          </div>
          <div className="h-64 w-full">
            <ResponsiveContainer width="100%" height="100%">
              <AreaChart data={stats?.requestsOverTime || []}>
                <defs>
                  <linearGradient id="requestColor" x1="0" y1="0" x2="0" y2="1">
                    <stop offset="5%" stopColor="#6366f1" stopOpacity={0.6} />
                    <stop offset="95%" stopColor="#6366f1" stopOpacity={0.0} />
                  </linearGradient>
                </defs>
                <XAxis dataKey="date" stroke="#64748b" fontSize={11} tickLine={false} />
                <YAxis stroke="#64748b" fontSize={11} tickLine={false} allowDecimals={false} />
                <Tooltip 
                  contentStyle={{ backgroundColor: '#1e293b', borderColor: '#334155', borderRadius: '12px', color: '#fff' }}
                />
                <Area type="monotone" dataKey="count" stroke="#6366f1" strokeWidth={3} fillOpacity={1} fill="url(#requestColor)" />
              </AreaChart>
            </ResponsiveContainer>
          </div>
        </div>

        {/* Requests by Status (Donut Chart) */}
        <div className="glass-panel p-6 rounded-3xl border border-slate-800/80 space-y-4 flex flex-col justify-between">
          <div>
            <h2 className="text-base font-bold text-white">Requests by Status</h2>
            <p className="text-xs text-slate-400">Distribution of current states</p>
          </div>
          <div className="h-48 w-full flex items-center justify-center">
            {stats?.requestsByStatus && stats.requestsByStatus.length > 0 ? (
              <ResponsiveContainer width="100%" height="100%">
                <PieChart>
                  <Pie
                    data={stats.requestsByStatus}
                    innerRadius={50}
                    outerRadius={75}
                    paddingAngle={4}
                    dataKey="value"
                  >
                    {stats.requestsByStatus.map((_, index) => (
                      <Cell key={`cell-${index}`} fill={statusColors[index % statusColors.length]} />
                    ))}
                  </Pie>
                  <Tooltip 
                    contentStyle={{ backgroundColor: '#1e293b', borderColor: '#334155', borderRadius: '12px', color: '#fff' }}
                  />
                </PieChart>
              </ResponsiveContainer>
            ) : (
              <p className="text-xs text-slate-500">No requests recorded yet.</p>
            )}
          </div>
          <div className="flex flex-wrap gap-2 justify-center">
            {stats?.requestsByStatus.map((s, idx) => (
              <span key={s.name} className="flex items-center gap-1.5 text-[11px] text-slate-300">
                <span className="w-2 h-2 rounded-full" style={{ backgroundColor: statusColors[idx % statusColors.length] }}></span>
                {s.name} ({s.value})
              </span>
            ))}
          </div>
        </div>
      </div>

      {/* Requests by Workflow Type (Bar Chart) */}
      <div className="glass-panel p-6 rounded-3xl border border-slate-800/80 space-y-4">
        <div className="flex items-center justify-between">
          <div>
            <h2 className="text-base font-bold text-white">Volume by Workflow Definition</h2>
            <p className="text-xs text-slate-400">Requests handled across configured approval pathways</p>
          </div>
          <Link to="/requests" className="text-xs font-semibold text-indigo-400 hover:text-indigo-300 flex items-center gap-1">
            <span>View All Requests</span>
            <ArrowRight className="w-3.5 h-3.5" />
          </Link>
        </div>
        <div className="h-60 w-full">
          {stats?.requestsByWorkflow && stats.requestsByWorkflow.length > 0 ? (
            <ResponsiveContainer width="100%" height="100%">
              <BarChart data={stats.requestsByWorkflow}>
                <XAxis dataKey="name" stroke="#64748b" fontSize={11} tickLine={false} />
                <YAxis stroke="#64748b" fontSize={11} tickLine={false} allowDecimals={false} />
                <Tooltip 
                  contentStyle={{ backgroundColor: '#1e293b', borderColor: '#334155', borderRadius: '12px', color: '#fff' }}
                />
                <Bar dataKey="value" fill="#818cf8" radius={[8, 8, 0, 0]} />
              </BarChart>
            </ResponsiveContainer>
          ) : (
            <div className="flex items-center justify-center h-full text-slate-500 text-xs">
              No workflow activity yet. Submit a request to populate charts.
            </div>
          )}
        </div>
      </div>
    </div>
  );
};
