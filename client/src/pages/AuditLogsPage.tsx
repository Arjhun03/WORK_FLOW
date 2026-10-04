import React, { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { 
  History, 
  ChevronLeft, 
  ChevronRight,
  ShieldAlert
} from 'lucide-react';
import { auditLogService } from '../services';

export const AuditLogsPage: React.FC = () => {
  const [page, setPage] = useState(1);
  const pageSize = 15;

  const { data, isLoading } = useQuery({
    queryKey: ['auditLogs', page],
    queryFn: () => auditLogService.getLogs({ page, pageSize }),
    refetchInterval: 12000
  });

  const getActionBadge = (action: string) => {
    if (action.includes('APPROVED')) return 'bg-emerald-500/20 text-emerald-300 border-emerald-500/30';
    if (action.includes('REJECTED')) return 'bg-rose-500/20 text-rose-300 border-rose-500/30';
    if (action.includes('CREATED')) return 'bg-indigo-500/20 text-indigo-300 border-indigo-500/30';
    if (action.includes('CANCELLED')) return 'bg-slate-700 text-slate-300 border-slate-600';
    return 'bg-purple-500/20 text-purple-300 border-purple-500/30';
  };

  return (
    <div className="space-y-6 max-w-7xl mx-auto">
      {/* Header */}
      <div>
        <h1 className="text-2xl font-bold text-white tracking-tight flex items-center gap-2.5">
          <History className="w-6 h-6 text-purple-400" />
          <span>System Audit Trail</span>
        </h1>
        <p className="text-slate-400 text-sm">
          Immutable event log of every request status transition, approval sign-off, and workflow alteration.
        </p>
      </div>

      {/* Audit Table */}
      <div className="glass-panel rounded-3xl border border-slate-800 overflow-hidden">
        <div className="overflow-x-auto">
          <table className="w-full text-left border-collapse">
            <thead>
              <tr className="border-b border-slate-800 bg-slate-900/50 text-[11px] font-bold text-slate-400 uppercase tracking-wider">
                <th className="py-3.5 px-6">Timestamp</th>
                <th className="py-3.5 px-6">Actor</th>
                <th className="py-3.5 px-6">Action Executed</th>
                <th className="py-3.5 px-6">State Transition</th>
                <th className="py-3.5 px-6">Target Request</th>
                <th className="py-3.5 px-6">Metadata Details</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-800/60 text-xs">
              {isLoading ? (
                <tr>
                  <td colSpan={6} className="py-12 text-center text-slate-400">Loading audit records...</td>
                </tr>
              ) : !data?.items || data.items.length === 0 ? (
                <tr>
                  <td colSpan={6} className="py-12 text-center text-slate-400">
                    <ShieldAlert className="w-6 h-6 mx-auto mb-2 text-slate-500" />
                    <span>No audit entries recorded yet.</span>
                  </td>
                </tr>
              ) : (
                data.items.map((log) => (
                  <tr key={log.id} className="hover:bg-slate-800/30 transition-colors">
                    <td className="py-4 px-6 text-slate-400 whitespace-nowrap">
                      {new Date(log.timestamp).toLocaleString()}
                    </td>
                    <td className="py-4 px-6 font-semibold text-white">
                      {log.userName}
                    </td>
                    <td className="py-4 px-6">
                      <span className={`px-2.5 py-0.5 rounded-full text-[10px] font-bold border ${getActionBadge(log.action)}`}>
                        {log.action}
                      </span>
                    </td>
                    <td className="py-4 px-6 text-slate-300">
                      {log.oldValue && log.newValue ? (
                        <span>
                          <span className="text-slate-500">{log.oldValue}</span>
                          <span className="text-slate-400 mx-1.5">➔</span>
                          <span className="font-semibold text-indigo-400">{log.newValue}</span>
                        </span>
                      ) : log.newValue ? (
                        <span className="text-indigo-400 font-semibold">{log.newValue}</span>
                      ) : (
                        <span className="text-slate-600">—</span>
                      )}
                    </td>
                    <td className="py-4 px-6 text-slate-400 font-mono text-[11px]">
                      {log.requestId ? log.requestId.substring(0, 8) + '...' : 'System'}
                    </td>
                    <td className="py-4 px-6 text-slate-400">
                      {log.metadata && Object.keys(log.metadata).length > 0 ? (
                        <pre className="text-[10px] bg-slate-900/80 p-1.5 rounded-lg border border-slate-800 max-w-xs overflow-x-auto font-mono">
                          {JSON.stringify(log.metadata)}
                        </pre>
                      ) : (
                        <span className="text-slate-600">—</span>
                      )}
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
