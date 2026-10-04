import React, { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { Link } from 'react-router-dom';
import { 
  FileText, 
  Plus, 
  Search, 
  Clock, 
  CheckCircle2, 
  XCircle, 
  Eye, 
  ChevronLeft, 
  ChevronRight,
  Filter
} from 'lucide-react';
import { requestService, workflowService } from '../services';

export const RequestsPage: React.FC = () => {
  const [status, setStatus] = useState<string>('');
  const [workflowId, setWorkflowId] = useState<string>('');
  const [search, setSearch] = useState<string>('');
  const [page, setPage] = useState<number>(1);
  const pageSize = 10;

  const { data: workflows = [] } = useQuery({
    queryKey: ['workflowsFilter'],
    queryFn: () => workflowService.getWorkflows()
  });

  const { data, isLoading } = useQuery({
    queryKey: ['requests', status, workflowId, search, page],
    queryFn: () => requestService.getRequests({
      status: status || undefined,
      workflowId: workflowId || undefined,
      search: search || undefined,
      page,
      pageSize
    }),
    refetchInterval: 10000
  });

  const getStatusBadge = (s: string) => {
    switch (s) {
      case 'Completed':
        return (
          <span className="inline-flex items-center gap-1.5 px-2.5 py-1 rounded-full text-xs font-semibold bg-emerald-500/20 text-emerald-300 border border-emerald-500/30">
            <CheckCircle2 className="w-3.5 h-3.5" />
            <span>Completed</span>
          </span>
        );
      case 'InProgress':
        return (
          <span className="inline-flex items-center gap-1.5 px-2.5 py-1 rounded-full text-xs font-semibold bg-blue-500/20 text-blue-300 border border-blue-500/30">
            <Clock className="w-3.5 h-3.5 animate-spin" />
            <span>In Progress</span>
          </span>
        );
      case 'Pending':
        return (
          <span className="inline-flex items-center gap-1.5 px-2.5 py-1 rounded-full text-xs font-semibold bg-amber-500/20 text-amber-300 border border-amber-500/30">
            <Clock className="w-3.5 h-3.5" />
            <span>Pending</span>
          </span>
        );
      case 'Rejected':
        return (
          <span className="inline-flex items-center gap-1.5 px-2.5 py-1 rounded-full text-xs font-semibold bg-rose-500/20 text-rose-300 border border-rose-500/30">
            <XCircle className="w-3.5 h-3.5" />
            <span>Rejected</span>
          </span>
        );
      case 'Cancelled':
        return (
          <span className="inline-flex items-center gap-1.5 px-2.5 py-1 rounded-full text-xs font-semibold bg-slate-700 text-slate-300">
            <span>Cancelled</span>
          </span>
        );
      default:
        return (
          <span className="px-2.5 py-1 rounded-full text-xs font-semibold bg-slate-800 text-slate-300">
            {s}
          </span>
        );
    }
  };

  return (
    <div className="space-y-6 max-w-7xl mx-auto">
      {/* Header */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div>
          <h1 className="text-2xl font-bold text-white tracking-tight flex items-center gap-2.5">
            <FileText className="w-6 h-6 text-indigo-400" />
            <span>Requests Directory</span>
          </h1>
          <p className="text-slate-400 text-sm">
            Monitor, submit, and track approval requests across their full multi-step lifecycle.
          </p>
        </div>

        <Link
          to="/requests/create"
          className="inline-flex items-center gap-2 px-4 py-2.5 bg-gradient-to-r from-indigo-600 to-indigo-500 hover:from-indigo-500 hover:to-indigo-400 text-white rounded-xl text-sm font-semibold shadow-lg shadow-indigo-600/25 transition-all"
        >
          <Plus className="w-4 h-4" />
          <span>New Request</span>
        </Link>
      </div>

      {/* Search and Filters Bar */}
      <div className="glass-panel p-4 rounded-2xl flex flex-col md:flex-row gap-3">
        <div className="flex-1 flex items-center gap-2 bg-slate-900/60 px-3 py-2 rounded-xl border border-slate-800">
          <Search className="w-4 h-4 text-slate-400" />
          <input
            type="text"
            value={search}
            onChange={(e) => { setSearch(e.target.value); setPage(1); }}
            placeholder="Search by title, requester, or description..."
            className="w-full bg-transparent text-xs text-white placeholder-slate-500 outline-none"
          />
        </div>

        <div className="flex flex-wrap items-center gap-3">
          <div className="flex items-center gap-2 bg-slate-900/60 px-3 py-2 rounded-xl border border-slate-800">
            <Filter className="w-3.5 h-3.5 text-slate-400" />
            <select
              value={status}
              onChange={(e) => { setStatus(e.target.value); setPage(1); }}
              className="bg-transparent text-xs text-white outline-none cursor-pointer"
            >
              <option value="" className="bg-slate-900">All Statuses</option>
              <option value="Pending" className="bg-slate-900">Pending</option>
              <option value="InProgress" className="bg-slate-900">In Progress</option>
              <option value="Completed" className="bg-slate-900">Completed</option>
              <option value="Rejected" className="bg-slate-900">Rejected</option>
              <option value="Cancelled" className="bg-slate-900">Cancelled</option>
            </select>
          </div>

          <div className="flex items-center gap-2 bg-slate-900/60 px-3 py-2 rounded-xl border border-slate-800">
            <select
              value={workflowId}
              onChange={(e) => { setWorkflowId(e.target.value); setPage(1); }}
              className="bg-transparent text-xs text-white outline-none cursor-pointer"
            >
              <option value="" className="bg-slate-900">All Workflows</option>
              {workflows.map((w) => (
                <option key={w.id} value={w.id} className="bg-slate-900">{w.name}</option>
              ))}
            </select>
          </div>
        </div>
      </div>

      {/* Requests Table */}
      <div className="glass-panel rounded-3xl border border-slate-800 overflow-hidden">
        <div className="overflow-x-auto">
          <table className="w-full text-left border-collapse">
            <thead>
              <tr className="border-b border-slate-800 bg-slate-900/50 text-[11px] font-bold text-slate-400 uppercase tracking-wider">
                <th className="py-3.5 px-6">Request Title</th>
                <th className="py-3.5 px-6">Workflow Type</th>
                <th className="py-3.5 px-6">Requester</th>
                <th className="py-3.5 px-6">Current Step</th>
                <th className="py-3.5 px-6">Status</th>
                <th className="py-3.5 px-6">Created</th>
                <th className="py-3.5 px-6 text-right">Actions</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-800/60 text-xs">
              {isLoading ? (
                <tr>
                  <td colSpan={7} className="py-12 text-center text-slate-400">Loading requests...</td>
                </tr>
              ) : !data?.items || data.items.length === 0 ? (
                <tr>
                  <td colSpan={7} className="py-12 text-center text-slate-400">
                    No matching requests found. Submit a new request to begin.
                  </td>
                </tr>
              ) : (
                data.items.map((req) => (
                  <tr key={req.id} className="hover:bg-slate-800/30 transition-colors">
                    <td className="py-4 px-6 font-semibold text-white">
                      <Link to={`/requests/${req.id}`} className="hover:text-indigo-400 transition-colors">
                        {req.title}
                      </Link>
                    </td>
                    <td className="py-4 px-6">
                      <span className="px-2 py-0.5 rounded-lg bg-indigo-500/10 text-indigo-300 font-medium">
                        {req.workflowName} (v{req.workflowVersion})
                      </span>
                    </td>
                    <td className="py-4 px-6 text-slate-300">
                      <div>{req.requesterName}</div>
                      <div className="text-[10px] text-slate-500">{req.requesterDepartment}</div>
                    </td>
                    <td className="py-4 px-6 text-slate-300">
                      {req.status === 'Completed' ? (
                        <span className="text-emerald-400 font-medium">Final Approval Done</span>
                      ) : req.status === 'Rejected' ? (
                        <span className="text-rose-400 font-medium">Declined</span>
                      ) : (
                        <div>
                          <span className="font-semibold text-white">Step {req.currentStepOrder}</span>
                          <div className="text-[10px] text-slate-400">
                            {req.currentApproverName ? `Approver: ${req.currentApproverName}` : `Role: ${req.currentApproverRole || 'Unassigned'}`}
                          </div>
                        </div>
                      )}
                    </td>
                    <td className="py-4 px-6">{getStatusBadge(req.status)}</td>
                    <td className="py-4 px-6 text-slate-400">
                      {new Date(req.createdAt).toLocaleDateString()}
                    </td>
                    <td className="py-4 px-6 text-right">
                      <Link
                        to={`/requests/${req.id}`}
                        className="inline-flex items-center gap-1 px-3 py-1.5 bg-slate-800 hover:bg-slate-700 text-slate-200 hover:text-white rounded-lg text-xs font-semibold transition-all"
                      >
                        <Eye className="w-3.5 h-3.5" />
                        <span>Timeline</span>
                      </Link>
                    </td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>

        {/* Pagination Bar */}
        {data && data.totalPages > 1 && (
          <div className="p-4 border-t border-slate-800 flex items-center justify-between text-xs text-slate-400">
            <span>
              Showing {((page - 1) * pageSize) + 1} - {Math.min(page * pageSize, data.totalCount)} of {data.totalCount}
            </span>
            <div className="flex items-center gap-2">
              <button
                disabled={page <= 1}
                onClick={() => setPage(p => Math.max(1, p - 1))}
                className="p-1.5 rounded-lg border border-slate-800 hover:bg-slate-800 disabled:opacity-40"
              >
                <ChevronLeft className="w-4 h-4" />
              </button>
              <span className="font-medium text-white px-2">Page {page} of {data.totalPages}</span>
              <button
                disabled={page >= data.totalPages}
                onClick={() => setPage(p => Math.min(data.totalPages, p + 1))}
                className="p-1.5 rounded-lg border border-slate-800 hover:bg-slate-800 disabled:opacity-40"
              >
                <ChevronRight className="w-4 h-4" />
              </button>
            </div>
          </div>
        )}
      </div>
    </div>
  );
};
