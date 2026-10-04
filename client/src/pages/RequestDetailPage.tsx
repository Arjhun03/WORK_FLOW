import React, { useState } from 'react';
import { useParams, Link } from 'react-router-dom';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { 
  ArrowLeft, 
  CheckCircle2, 
  Clock, 
  XCircle, 
  MessageSquare, 
  History, 
  Check, 
  X, 
  Send,
  AlertCircle,
  Edit3
} from 'lucide-react';
import { requestService } from '../services';
import { useAuth } from '../context/AuthContext';

export const RequestDetailPage: React.FC = () => {
  const { id } = useParams<{ id: string }>();
  const { user } = useAuth();
  const queryClient = useQueryClient();

  const [commentText, setCommentText] = useState('');
  const [actionComments, setActionComments] = useState('');
  const [modalAction, setModalAction] = useState<'approve' | 'reject' | null>(null);
  const [actionError, setActionError] = useState<string | null>(null);

  // Edit Request Details state
  const [showEditModal, setShowEditModal] = useState(false);
  const [editTitle, setEditTitle] = useState('');
  const [editDescription, setEditDescription] = useState('');
  const [editUrgency, setEditUrgency] = useState('Medium');
  const [editJustification, setEditJustification] = useState('');
  const [editError, setEditError] = useState<string | null>(null);

  const { data: request, isLoading } = useQuery({
    queryKey: ['requestDetail', id],
    queryFn: () => requestService.getRequestById(id!),
    refetchInterval: 8000,
    enabled: Boolean(id)
  });

  // Comment Mutation
  const commentMutation = useMutation({
    mutationFn: (msg: string) => requestService.addComment(id!, msg),
    onSuccess: () => {
      setCommentText('');
      queryClient.invalidateQueries({ queryKey: ['requestDetail', id] });
    }
  });

  // Approve Mutation
  const approveMutation = useMutation({
    mutationFn: (comments?: string) => requestService.approveRequest(id!, comments),
    onSuccess: () => {
      setModalAction(null);
      setActionComments('');
      queryClient.invalidateQueries({ queryKey: ['requestDetail', id] });
      queryClient.invalidateQueries({ queryKey: ['pendingApprovalsCount'] });
    },
    onError: (err: any) => {
      setActionError(err.response?.data?.message || 'Approval failed.');
    }
  });

  // Reject Mutation
  const rejectMutation = useMutation({
    mutationFn: (comments?: string) => requestService.rejectRequest(id!, comments),
    onSuccess: () => {
      setModalAction(null);
      setActionComments('');
      queryClient.invalidateQueries({ queryKey: ['requestDetail', id] });
      queryClient.invalidateQueries({ queryKey: ['pendingApprovalsCount'] });
    },
    onError: (err: any) => {
      setActionError(err.response?.data?.message || 'Rejection failed.');
    }
  });

  // Cancel Mutation
  const cancelMutation = useMutation({
    mutationFn: () => requestService.cancelRequest(id!),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['requestDetail', id] });
    }
  });

  // Update Request Details Mutation
  const updateMutation = useMutation({
    mutationFn: (payload: { title?: string; description?: string; data?: Record<string, any> }) =>
      requestService.updateRequest(id!, payload),
    onSuccess: () => {
      setShowEditModal(false);
      queryClient.invalidateQueries({ queryKey: ['requestDetail', id] });
      queryClient.invalidateQueries({ queryKey: ['requests'] });
    },
    onError: (err: any) => {
      setEditError(err.response?.data?.message || 'Failed to update request details in MongoDB.');
    }
  });

  const handleOpenEdit = () => {
    if (!request) return;
    setEditTitle(request.title);
    setEditDescription(request.description);
    setEditUrgency((request.data?.urgency as string) || 'Medium');
    setEditJustification((request.data?.justification as string) || '');
    setEditError(null);
    setShowEditModal(true);
  };

  if (isLoading || !request) {
    return (
      <div className="py-24 text-center text-slate-400 text-sm">
        <div className="w-8 h-8 border-4 border-indigo-500 border-t-transparent rounded-full animate-spin mx-auto mb-3"></div>
        <span>Loading request details...</span>
      </div>
    );
  }

  // Check if current user is eligible to act on current pending approval
  const isTerminal = ['Completed', 'Approved', 'Rejected', 'Cancelled'].includes(request.status);
  const currentStep = request.workflowSteps?.find(s => s.order === request.currentStepOrder);
  const pendingApproval = request.approvals?.find(a => a.stepOrder === request.currentStepOrder && a.status === 'Pending');

  const canApprove = !isTerminal && pendingApproval && user && (
    user.roles.includes('Admin') ||
    pendingApproval.approverId === user.id ||
    (pendingApproval.approverRole && user.roles.includes(pendingApproval.approverRole))
  ) && (request.requestedBy !== user.id || user.roles.includes('Admin'));

  const canEdit = !isTerminal && (request.requestedBy === user?.id || user?.roles.includes('Admin'));
  const canCancel = !isTerminal && (request.requestedBy === user?.id || user?.roles.includes('Admin'));

  return (
    <div className="space-y-8 max-w-5xl mx-auto">
      {/* Header */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div className="flex items-center gap-3">
          <Link
            to="/requests"
            className="p-2 glass-card hover:bg-slate-800 text-slate-300 rounded-xl transition-all"
          >
            <ArrowLeft className="w-4 h-4" />
          </Link>
          <div>
            <div className="flex items-center gap-2 mb-1">
              <span className="text-xs px-2 py-0.5 rounded-md bg-indigo-500/20 text-indigo-300 font-semibold">
                {request.requestType} • {request.workflowName} (v{request.workflowVersion})
              </span>
              <span className={`text-xs px-2.5 py-0.5 rounded-full font-bold ${
                request.status === 'Completed' ? 'bg-emerald-500/20 text-emerald-300 border border-emerald-500/30' :
                request.status === 'Rejected' ? 'bg-rose-500/20 text-rose-300 border border-rose-500/30' :
                request.status === 'Cancelled' ? 'bg-slate-800 text-slate-400' :
                'bg-blue-500/20 text-blue-300 border border-blue-500/30 animate-pulse'
              }`}>
                {request.status}
              </span>
            </div>
            <h1 className="text-2xl font-bold text-white tracking-tight">{request.title}</h1>
          </div>
        </div>

        {/* Action Controls */}
        <div className="flex items-center gap-2">
          {canApprove && (
            <>
              <button
                type="button"
                onClick={() => { setActionError(null); setModalAction('approve'); }}
                className="flex items-center gap-1.5 px-4 py-2 bg-emerald-600 hover:bg-emerald-500 text-white rounded-xl text-xs font-bold shadow-lg shadow-emerald-600/30 transition-all"
              >
                <Check className="w-4 h-4" />
                <span>Approve Step</span>
              </button>
              <button
                type="button"
                onClick={() => { setActionError(null); setModalAction('reject'); }}
                className="flex items-center gap-1.5 px-4 py-2 bg-rose-600 hover:bg-rose-500 text-white rounded-xl text-xs font-bold shadow-lg shadow-rose-600/30 transition-all"
              >
                <X className="w-4 h-4" />
                <span>Reject</span>
              </button>
            </>
          )}

          {canEdit && (
            <button
              type="button"
              onClick={handleOpenEdit}
              className="flex items-center gap-1.5 px-3 py-2 bg-indigo-600/80 hover:bg-indigo-600 text-white rounded-xl text-xs font-semibold shadow-md shadow-indigo-600/20 transition-all"
            >
              <Edit3 className="w-3.5 h-3.5" />
              <span>Edit Details</span>
            </button>
          )}

          {canCancel && (
            <button
              type="button"
              onClick={() => {
                if (confirm('Are you sure you want to cancel this request?')) {
                  cancelMutation.mutate();
                }
              }}
              className="px-3 py-2 glass-card hover:bg-slate-800 text-slate-400 hover:text-rose-400 rounded-xl text-xs font-semibold transition-all"
            >
              Cancel Request
            </button>
          )}
        </div>
      </div>

      {/* Main Details & Visual Timeline */}
      <div className="grid lg:grid-cols-3 gap-6">
        {/* Left 2 Cols: Description, Data, and Visual Timeline */}
        <div className="lg:col-span-2 space-y-6">
          {/* Request Info Card */}
          <div className="glass-panel p-6 rounded-3xl border border-slate-800 space-y-4">
            <h2 className="text-xs font-bold uppercase tracking-wider text-indigo-400">Request Details</h2>
            <p className="text-sm text-slate-300 leading-relaxed whitespace-pre-wrap">{request.description}</p>

            <div className="grid sm:grid-cols-3 gap-3 pt-3 border-t border-slate-800/80 text-xs">
              <div>
                <span className="text-slate-500 block">Submitted By</span>
                <span className="font-semibold text-white">{request.requesterName}</span>
                <span className="text-[10px] text-slate-400 block">{request.requesterDepartment}</span>
              </div>
              <div>
                <span className="text-slate-500 block">Date Submitted</span>
                <span className="font-semibold text-white">{new Date(request.createdAt).toLocaleString()}</span>
              </div>
              <div>
                <span className="text-slate-500 block">Current Step</span>
                <span className="font-semibold text-indigo-300">
                  {request.status === 'Completed' ? 'Finished' : `Step ${request.currentStepOrder}`}
                </span>
                {request.currentApproverName && (
                  <span className="text-[10px] text-slate-400 block">Waiting on: {request.currentApproverName}</span>
                )}
              </div>
            </div>

            {request.data && Object.keys(request.data).length > 0 && (
              <div className="pt-3 border-t border-slate-800/80">
                <span className="text-xs font-bold text-slate-400 block mb-2">Form Data Metadata:</span>
                <div className="grid grid-cols-2 gap-2 text-xs">
                  {Object.entries(request.data).map(([key, val]) => (
                    <div key={key} className="p-2 bg-slate-900/60 rounded-lg border border-slate-800">
                      <span className="text-slate-500 capitalize">{key}: </span>
                      <span className="text-slate-200 font-medium">{String(val)}</span>
                    </div>
                  ))}
                </div>
              </div>
            )}
          </div>

          {/* SECTION 31: Dynamic Visual Approval Timeline */}
          <div className="glass-panel p-6 rounded-3xl border border-slate-800 space-y-6">
            <h2 className="text-xs font-bold uppercase tracking-wider text-indigo-400">
              Approval Flow Timeline
            </h2>

            <div className="relative pl-6 space-y-8 before:absolute before:left-2.5 before:top-3 before:bottom-3 before:w-0.5 before:bg-slate-800">
              {/* Submission Node */}
              <div className="relative">
                <div className="absolute -left-[27px] top-0 w-6 h-6 rounded-full bg-emerald-500 text-white flex items-center justify-center shadow-md">
                  <CheckCircle2 className="w-3.5 h-3.5" />
                </div>
                <div>
                  <div className="flex items-center justify-between">
                    <h3 className="text-xs font-bold text-white">Request Created & Submitted</h3>
                    <span className="text-[10px] text-slate-500">{new Date(request.createdAt).toLocaleTimeString()}</span>
                  </div>
                  <p className="text-[11px] text-slate-400">By {request.requesterName} ({request.requesterDepartment})</p>
                </div>
              </div>

              {/* Dynamic Steps Nodes */}
              {request.workflowSteps?.map((step) => {
                const approvalRecord = request.approvals?.find(a => a.stepOrder === step.order);
                const isStepApproved = approvalRecord?.status === 'Approved';
                const isStepRejected = approvalRecord?.status === 'Rejected';
                const isStepCurrent = request.currentStepOrder === step.order && !isTerminal;
                const isStepPendingFuture = step.order > request.currentStepOrder;

                let iconColor = 'bg-slate-800 border-slate-700 text-slate-500';
                let Icon = Clock;

                if (isStepApproved) {
                  iconColor = 'bg-emerald-500 text-white';
                  Icon = CheckCircle2;
                } else if (isStepRejected) {
                  iconColor = 'bg-rose-500 text-white';
                  Icon = XCircle;
                } else if (isStepCurrent) {
                  iconColor = 'bg-indigo-600 text-white ring-4 ring-indigo-500/20 animate-pulse';
                  Icon = Clock;
                }

                return (
                  <div key={step.name + step.order} className="relative">
                    <div className={`absolute -left-[27px] top-0 w-6 h-6 rounded-full flex items-center justify-center shadow-md ${iconColor}`}>
                      <Icon className="w-3.5 h-3.5" />
                    </div>

                    <div className="p-3.5 bg-slate-900/60 rounded-xl border border-slate-800/80 space-y-1.5">
                      <div className="flex items-center justify-between">
                        <h4 className="text-xs font-bold text-white">
                          Step {step.order}: {step.name}
                        </h4>
                        <span className={`text-[10px] font-semibold px-2 py-0.5 rounded-full ${
                          isStepApproved ? 'bg-emerald-500/20 text-emerald-300' :
                          isStepRejected ? 'bg-rose-500/20 text-rose-300' :
                          isStepCurrent ? 'bg-indigo-500/20 text-indigo-300' :
                          'bg-slate-800 text-slate-500'
                        }`}>
                          {approvalRecord ? approvalRecord.status : isStepPendingFuture ? 'Pending' : 'Skipped'}
                        </span>
                      </div>

                      <div className="text-[11px] text-slate-400 flex flex-wrap gap-x-4">
                        <span>Strategy: <strong className="text-slate-300">{step.approverType}</strong></span>
                        {approvalRecord?.approverName && (
                          <span>Approver: <strong className="text-white">{approvalRecord.approverName}</strong></span>
                        )}
                        {approvalRecord?.actionDate && (
                          <span>Date: {new Date(approvalRecord.actionDate).toLocaleString()}</span>
                        )}
                      </div>

                      {approvalRecord?.comments && (
                        <div className="mt-2 p-2.5 bg-slate-950/70 border border-slate-800 rounded-lg text-xs text-slate-300 italic">
                          "{approvalRecord.comments}"
                        </div>
                      )}
                    </div>
                  </div>
                );
              })}

              {/* Completion Node */}
              <div className="relative">
                <div className={`absolute -left-[27px] top-0 w-6 h-6 rounded-full flex items-center justify-center shadow-md ${
                  request.status === 'Completed' ? 'bg-emerald-500 text-white' :
                  request.status === 'Rejected' ? 'bg-rose-500 text-white' :
                  'bg-slate-800 text-slate-500'
                }`}>
                  {request.status === 'Completed' ? <CheckCircle2 className="w-3.5 h-3.5" /> :
                   request.status === 'Rejected' ? <XCircle className="w-3.5 h-3.5" /> :
                   <Clock className="w-3.5 h-3.5" />}
                </div>
                <div>
                  <h4 className="text-xs font-bold text-white">
                    {request.status === 'Completed' ? 'Workflow Completed Successfully' :
                     request.status === 'Rejected' ? 'Workflow Terminated (Rejected)' :
                     'Final Step Pending'}
                  </h4>
                  {request.completedAt && (
                    <p className="text-[11px] text-slate-500">{new Date(request.completedAt).toLocaleString()}</p>
                  )}
                </div>
              </div>
            </div>
          </div>
        </div>

        {/* Right Column: Real-time Comments & Audit Trail */}
        <div className="space-y-6">
          {/* Comments Panel */}
          <div className="glass-panel p-5 rounded-3xl border border-slate-800 flex flex-col h-[420px]">
            <div className="flex items-center gap-2 mb-3 pb-3 border-b border-slate-800">
              <MessageSquare className="w-4 h-4 text-indigo-400" />
              <h2 className="text-xs font-bold uppercase tracking-wider text-white">Discussion Thread</h2>
            </div>

            <div className="flex-1 overflow-y-auto space-y-3 pr-1 text-xs">
              {request.comments?.length === 0 ? (
                <div className="text-center py-12 text-slate-500 text-xs">No comments yet. Start the conversation.</div>
              ) : (
                request.comments?.map((c) => (
                  <div key={c.id} className="p-3 bg-slate-900/80 rounded-xl border border-slate-800/80 space-y-1">
                    <div className="flex items-center justify-between text-[11px]">
                      <span className="font-bold text-indigo-300">{c.userName}</span>
                      <span className="text-slate-500">{new Date(c.createdAt).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}</span>
                    </div>
                    <p className="text-slate-300 text-xs leading-relaxed">{c.message}</p>
                  </div>
                ))
              )}
            </div>

            {/* Add Comment Input */}
            <form
              onSubmit={(e) => {
                e.preventDefault();
                if (commentText.trim()) commentMutation.mutate(commentText);
              }}
              className="mt-3 pt-3 border-t border-slate-800 flex items-center gap-2"
            >
              <input
                type="text"
                value={commentText}
                onChange={(e) => setCommentText(e.target.value)}
                placeholder="Ask or reply to approvers..."
                className="flex-1 bg-slate-900 border border-slate-800 rounded-xl px-3 py-2 text-xs text-white placeholder-slate-500 outline-none focus:border-indigo-500"
              />
              <button
                type="submit"
                disabled={commentMutation.isPending || !commentText.trim()}
                className="p-2 bg-indigo-600 hover:bg-indigo-500 text-white rounded-xl transition-all disabled:opacity-40"
              >
                <Send className="w-3.5 h-3.5" />
              </button>
            </form>
          </div>

          {/* Audit Logs Trail */}
          <div className="glass-panel p-5 rounded-3xl border border-slate-800 space-y-3">
            <div className="flex items-center gap-2 pb-2 border-b border-slate-800">
              <History className="w-4 h-4 text-purple-400" />
              <h2 className="text-xs font-bold uppercase tracking-wider text-white">Immutable Audit Trail</h2>
            </div>
            <div className="space-y-2.5 max-h-56 overflow-y-auto pr-1 text-xs">
              {request.auditLogs?.map((log) => (
                <div key={log.id} className="p-2.5 bg-slate-900/60 rounded-xl border border-slate-800/80 text-[11px] space-y-1">
                  <div className="flex items-center justify-between">
                    <span className="font-semibold text-slate-300">{log.action}</span>
                    <span className="text-[10px] text-slate-500">{new Date(log.timestamp).toLocaleTimeString()}</span>
                  </div>
                  <p className="text-slate-400">Actor: <strong className="text-slate-200">{log.userName}</strong></p>
                  {log.oldValue && log.newValue && (
                    <div className="text-[10px] text-slate-500">
                      {log.oldValue} ➔ <span className="text-indigo-400">{log.newValue}</span>
                    </div>
                  )}
                </div>
              ))}
            </div>
          </div>
        </div>
      </div>

      {/* Modal Dialog for Approve / Reject Action */}
      {modalAction && (
        <div className="fixed inset-0 z-50 bg-black/70 backdrop-blur-sm flex items-center justify-center p-4">
          <div className="glass-panel p-6 rounded-3xl max-w-md w-full border border-slate-700 shadow-2xl space-y-4">
            <h3 className="text-base font-bold text-white flex items-center gap-2">
              {modalAction === 'approve' ? (
                <>
                  <CheckCircle2 className="w-5 h-5 text-emerald-400" />
                  <span>Approve Step {request.currentStepOrder}</span>
                </>
              ) : (
                <>
                  <XCircle className="w-5 h-5 text-rose-400" />
                  <span>Reject Request</span>
                </>
              )}
            </h3>

            <p className="text-xs text-slate-400">
              {modalAction === 'approve'
                ? `You are confirming approval for step '${currentStep?.name}'. This will advance the workflow.`
                : 'Rejecting this request will permanently decline and terminate further approval steps.'}
            </p>

            {actionError && (
              <div className="p-3 bg-rose-500/10 border border-rose-500/20 text-rose-300 text-xs rounded-xl flex items-center gap-2">
                <AlertCircle className="w-4 h-4 shrink-0 text-rose-400" />
                <span>{actionError}</span>
              </div>
            )}

            <div>
              <label className="block text-xs font-semibold text-slate-300 mb-1">
                {modalAction === 'reject' ? 'Rejection Reason (Required)' : 'Comments / Sign-off Note (Optional)'}
              </label>
              <textarea
                rows={3}
                required={modalAction === 'reject'}
                value={actionComments}
                onChange={(e) => setActionComments(e.target.value)}
                placeholder={modalAction === 'reject' ? 'State clearly why this request cannot proceed...' : 'e.g. Verified budget and approved.'}
                className="w-full bg-slate-900 border border-slate-700 rounded-xl p-3 text-xs text-white outline-none focus:border-indigo-500"
              />
            </div>

            <div className="flex items-center justify-end gap-2 pt-2">
              <button
                type="button"
                onClick={() => setModalAction(null)}
                className="px-4 py-2 glass-card hover:bg-slate-800 text-slate-300 rounded-xl text-xs font-semibold"
              >
                Cancel
              </button>
              <button
                type="button"
                disabled={approveMutation.isPending || rejectMutation.isPending || (modalAction === 'reject' && !actionComments.trim())}
                onClick={() => {
                  if (modalAction === 'approve') {
                    approveMutation.mutate(actionComments);
                  } else {
                    rejectMutation.mutate(actionComments);
                  }
                }}
                className={`px-5 py-2 text-white rounded-xl text-xs font-bold shadow-lg transition-all disabled:opacity-50 ${
                  modalAction === 'approve'
                    ? 'bg-emerald-600 hover:bg-emerald-500 shadow-emerald-600/30'
                    : 'bg-rose-600 hover:bg-rose-500 shadow-rose-600/30'
                }`}
              >
                {modalAction === 'approve' ? 'Confirm Approval' : 'Confirm Rejection'}
              </button>
            </div>
          </div>
        </div>
      )}

      {/* Modal Dialog for Editing Request Details */}
      {showEditModal && (
        <div className="fixed inset-0 z-50 bg-black/70 backdrop-blur-sm flex items-center justify-center p-4">
          <div className="glass-panel p-6 rounded-3xl max-w-lg w-full border border-slate-700 shadow-2xl space-y-4">
            <div className="flex items-center justify-between pb-2 border-b border-slate-800">
              <h3 className="text-base font-bold text-white flex items-center gap-2">
                <Edit3 className="w-5 h-5 text-indigo-400" />
                <span>Edit Request Details</span>
              </h3>
              <button
                type="button"
                onClick={() => setShowEditModal(false)}
                className="p-1 text-slate-400 hover:text-white rounded-lg transition-colors"
              >
                <X className="w-4 h-4" />
              </button>
            </div>

            <p className="text-xs text-slate-400">
              Update request information and form details. Changes are persisted directly to MongoDB.
            </p>

            {editError && (
              <div className="p-3 bg-rose-500/10 border border-rose-500/20 text-rose-300 text-xs rounded-xl flex items-center gap-2">
                <AlertCircle className="w-4 h-4 shrink-0 text-rose-400" />
                <span>{editError}</span>
              </div>
            )}

            <form
              onSubmit={(e) => {
                e.preventDefault();
                setEditError(null);
                if (!editTitle.trim()) {
                  setEditError('Title is required.');
                  return;
                }
                updateMutation.mutate({
                  title: editTitle,
                  description: editDescription,
                  data: {
                    ...request.data,
                    urgency: editUrgency,
                    justification: editJustification
                  }
                });
              }}
              className="space-y-4"
            >
              <div>
                <label className="block text-xs font-semibold text-slate-300 mb-1">Request Title</label>
                <input
                  type="text"
                  required
                  value={editTitle}
                  onChange={(e) => setEditTitle(e.target.value)}
                  className="w-full bg-slate-900 border border-slate-700 rounded-xl px-3.5 py-2 text-xs text-white outline-none focus:border-indigo-500 transition-all"
                />
              </div>

              <div>
                <label className="block text-xs font-semibold text-slate-300 mb-1">Description & Purpose</label>
                <textarea
                  rows={3}
                  required
                  value={editDescription}
                  onChange={(e) => setEditDescription(e.target.value)}
                  className="w-full bg-slate-900 border border-slate-700 rounded-xl px-3.5 py-2 text-xs text-white outline-none focus:border-indigo-500 transition-all"
                />
              </div>

              <div className="grid grid-cols-2 gap-3">
                <div>
                  <label className="block text-xs font-semibold text-slate-300 mb-1">Urgency Level</label>
                  <select
                    value={editUrgency}
                    onChange={(e) => setEditUrgency(e.target.value)}
                    className="w-full bg-slate-900 border border-slate-700 rounded-xl px-3.5 py-2 text-xs text-white outline-none focus:border-indigo-500 transition-all cursor-pointer"
                  >
                    <option value="Low">Low</option>
                    <option value="Medium">Medium</option>
                    <option value="High">High</option>
                  </select>
                </div>

                <div>
                  <label className="block text-xs font-semibold text-slate-300 mb-1">Reference Code / Justification</label>
                  <input
                    type="text"
                    value={editJustification}
                    onChange={(e) => setEditJustification(e.target.value)}
                    placeholder="e.g. Project ID or reason"
                    className="w-full bg-slate-900 border border-slate-700 rounded-xl px-3.5 py-2 text-xs text-white outline-none focus:border-indigo-500 transition-all"
                  />
                </div>
              </div>

              <div className="flex items-center justify-end gap-2 pt-3 border-t border-slate-800">
                <button
                  type="button"
                  onClick={() => setShowEditModal(false)}
                  className="px-4 py-2 glass-card hover:bg-slate-800 text-slate-300 rounded-xl text-xs font-semibold"
                >
                  Cancel
                </button>
                <button
                  type="submit"
                  disabled={updateMutation.isPending}
                  className="px-5 py-2 bg-gradient-to-r from-indigo-600 to-indigo-500 hover:from-indigo-500 hover:to-indigo-400 text-white rounded-xl text-xs font-bold shadow-lg shadow-indigo-600/30 transition-all disabled:opacity-50 flex items-center gap-1.5"
                >
                  <Send className="w-3.5 h-3.5" />
                  <span>{updateMutation.isPending ? 'Saving...' : 'Save Changes'}</span>
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
};
