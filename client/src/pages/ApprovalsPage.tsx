import React, { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { Link } from 'react-router-dom';
import { 
  CheckSquare, 
  Check, 
  X, 
  Clock, 
  AlertCircle,
  Eye
} from 'lucide-react';
import { approvalService, requestService } from '../services';
import type { Approval } from '../types';

export const ApprovalsPage: React.FC = () => {
  const queryClient = useQueryClient();
  const [selectedApproval, setSelectedApproval] = useState<Approval | null>(null);
  const [actionType, setActionType] = useState<'approve' | 'reject' | null>(null);
  const [comments, setComments] = useState('');
  const [error, setError] = useState<string | null>(null);

  const { data: pending = [], isLoading } = useQuery({
    queryKey: ['pendingApprovals'],
    queryFn: () => approvalService.getPending(),
    refetchInterval: 10000
  });

  const approveMutation = useMutation({
    mutationFn: ({ requestId, comments }: { requestId: string; comments?: string }) =>
      requestService.approveRequest(requestId, comments),
    onSuccess: () => {
      setSelectedApproval(null);
      setActionType(null);
      setComments('');
      queryClient.invalidateQueries({ queryKey: ['pendingApprovals'] });
      queryClient.invalidateQueries({ queryKey: ['pendingApprovalsCount'] });
      queryClient.invalidateQueries({ queryKey: ['dashboardStats'] });
    },
    onError: (err: any) => {
      setError(err.response?.data?.message || 'Approval action failed.');
    }
  });

  const rejectMutation = useMutation({
    mutationFn: ({ requestId, comments }: { requestId: string; comments?: string }) =>
      requestService.rejectRequest(requestId, comments),
    onSuccess: () => {
      setSelectedApproval(null);
      setActionType(null);
      setComments('');
      queryClient.invalidateQueries({ queryKey: ['pendingApprovals'] });
      queryClient.invalidateQueries({ queryKey: ['pendingApprovalsCount'] });
      queryClient.invalidateQueries({ queryKey: ['dashboardStats'] });
    },
    onError: (err: any) => {
      setError(err.response?.data?.message || 'Rejection action failed.');
    }
  });

  return (
    <div className="space-y-6 max-w-7xl mx-auto">
      {/* Header */}
      <div>
        <h1 className="text-2xl font-bold text-white tracking-tight flex items-center gap-2.5">
          <CheckSquare className="w-6 h-6 text-amber-400" />
          <span>Pending Approvals Queue</span>
        </h1>
        <p className="text-slate-400 text-sm">
          Review requests specifically assigned to you or waiting on your department role.
        </p>
      </div>

      {isLoading ? (
        <div className="py-12 text-center text-slate-400 text-sm">Loading pending queue...</div>
      ) : pending.length === 0 ? (
        <div className="glass-panel p-12 rounded-3xl text-center space-y-3">
          <div className="w-12 h-12 rounded-full bg-emerald-500/10 text-emerald-400 flex items-center justify-center mx-auto">
            <CheckSquare className="w-6 h-6" />
          </div>
          <h3 className="text-base font-bold text-white">Your Approval Queue is Clear</h3>
          <p className="text-xs text-slate-400">
            No requests currently require your sign-off. You'll receive a notification when a new request arrives.
          </p>
        </div>
      ) : (
        <div className="grid md:grid-cols-2 lg:grid-cols-3 gap-6">
          {pending.map((item) => (
            <div key={item.id} className="glass-panel p-6 rounded-3xl border border-slate-800 flex flex-col justify-between space-y-5 hover:border-slate-700 transition-all">
              <div>
                <div className="flex items-center justify-between mb-2">
                  <span className="text-[11px] px-2 py-0.5 rounded-md bg-amber-500/20 text-amber-300 font-bold border border-amber-500/30 flex items-center gap-1">
                    <Clock className="w-3 h-3" />
                    <span>Awaiting Action</span>
                  </span>
                  <span className="text-[11px] text-slate-500">
                    Step {item.stepOrder}
                  </span>
                </div>

                <h3 className="text-base font-bold text-white mb-1">
                  {item.requestTitle || 'Approval Request'}
                </h3>
                <p className="text-xs text-indigo-300 font-medium mb-3">
                  Step: {item.stepName}
                </p>

                <div className="p-3 bg-slate-900/60 rounded-xl border border-slate-800/80 text-xs space-y-1 mb-2">
                  <div className="flex justify-between">
                    <span className="text-slate-500">Requested By:</span>
                    <span className="text-slate-200 font-semibold">{item.requesterName}</span>
                  </div>
                  <div className="flex justify-between">
                    <span className="text-slate-500">Assigned Role:</span>
                    <span className="text-indigo-400 font-medium">{item.approverRole || 'Individual'}</span>
                  </div>
                  <div className="flex justify-between">
                    <span className="text-slate-500">Submitted:</span>
                    <span className="text-slate-400">{new Date(item.createdAt).toLocaleDateString()}</span>
                  </div>
                </div>
              </div>

              {/* Action Buttons */}
              <div className="space-y-2 pt-2 border-t border-slate-800">
                <div className="grid grid-cols-2 gap-2">
                  <button
                    type="button"
                    onClick={() => {
                      setError(null);
                      setSelectedApproval(item);
                      setActionType('approve');
                    }}
                    className="flex items-center justify-center gap-1.5 py-2 bg-emerald-600 hover:bg-emerald-500 text-white rounded-xl text-xs font-bold transition-all shadow-md shadow-emerald-600/20"
                  >
                    <Check className="w-3.5 h-3.5" />
                    <span>Approve</span>
                  </button>
                  <button
                    type="button"
                    onClick={() => {
                      setError(null);
                      setSelectedApproval(item);
                      setActionType('reject');
                    }}
                    className="flex items-center justify-center gap-1.5 py-2 bg-rose-600 hover:bg-rose-500 text-white rounded-xl text-xs font-bold transition-all shadow-md shadow-rose-600/20"
                  >
                    <X className="w-3.5 h-3.5" />
                    <span>Reject</span>
                  </button>
                </div>

                <Link
                  to={`/requests/${item.requestId}`}
                  className="w-full flex items-center justify-center gap-1.5 py-2 glass-card hover:bg-slate-800 text-slate-300 rounded-xl text-xs font-semibold transition-all"
                >
                  <Eye className="w-3.5 h-3.5 text-indigo-400" />
                  <span>View Timeline & Details</span>
                </Link>
              </div>
            </div>
          ))}
        </div>
      )}

      {/* Decision Modal */}
      {selectedApproval && actionType && (
        <div className="fixed inset-0 z-50 bg-black/70 backdrop-blur-sm flex items-center justify-center p-4">
          <div className="glass-panel p-6 rounded-3xl max-w-md w-full border border-slate-700 shadow-2xl space-y-4">
            <h3 className="text-base font-bold text-white flex items-center gap-2">
              {actionType === 'approve' ? (
                <>
                  <Check className="w-5 h-5 text-emerald-400" />
                  <span>Approve: {selectedApproval.stepName}</span>
                </>
              ) : (
                <>
                  <X className="w-5 h-5 text-rose-400" />
                  <span>Reject: {selectedApproval.stepName}</span>
                </>
              )}
            </h3>

            <p className="text-xs text-slate-400">
              Request: <strong className="text-white">{selectedApproval.requestTitle}</strong> by {selectedApproval.requesterName}
            </p>

            {error && (
              <div className="p-3 bg-rose-500/10 border border-rose-500/20 text-rose-300 text-xs rounded-xl flex items-center gap-2">
                <AlertCircle className="w-4 h-4 shrink-0 text-rose-400" />
                <span>{error}</span>
              </div>
            )}

            <div>
              <label className="block text-xs font-semibold text-slate-300 mb-1">
                {actionType === 'reject' ? 'Rejection Reason (Required)' : 'Comments / Sign-off (Optional)'}
              </label>
              <textarea
                rows={3}
                required={actionType === 'reject'}
                value={comments}
                onChange={(e) => setComments(e.target.value)}
                placeholder={actionType === 'reject' ? 'Specify why this request cannot be approved...' : 'Optional approval notes...'}
                className="w-full bg-slate-900 border border-slate-700 rounded-xl p-3 text-xs text-white outline-none focus:border-indigo-500"
              />
            </div>

            <div className="flex items-center justify-end gap-2 pt-2">
              <button
                type="button"
                onClick={() => { setSelectedApproval(null); setActionType(null); }}
                className="px-4 py-2 glass-card hover:bg-slate-800 text-slate-300 rounded-xl text-xs font-semibold"
              >
                Cancel
              </button>
              <button
                type="button"
                disabled={approveMutation.isPending || rejectMutation.isPending || (actionType === 'reject' && !comments.trim())}
                onClick={() => {
                  if (actionType === 'approve') {
                    approveMutation.mutate({ requestId: selectedApproval.requestId, comments });
                  } else {
                    rejectMutation.mutate({ requestId: selectedApproval.requestId, comments });
                  }
                }}
                className={`px-5 py-2 text-white rounded-xl text-xs font-bold shadow-lg transition-all disabled:opacity-50 ${
                  actionType === 'approve'
                    ? 'bg-emerald-600 hover:bg-emerald-500 shadow-emerald-600/30'
                    : 'bg-rose-600 hover:bg-rose-500 shadow-rose-600/30'
                }`}
              >
                {actionType === 'approve' ? 'Confirm Approval' : 'Confirm Rejection'}
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};
