import React, { useState } from 'react';
import { useNavigate, Link } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import { 
  FileText, 
  ArrowLeft, 
  Send, 
  AlertCircle, 
  ArrowRight,
  ShieldCheck
} from 'lucide-react';
import { workflowService, requestService } from '../services';

export const CreateRequestPage: React.FC = () => {
  const navigate = useNavigate();
  const [workflowId, setWorkflowId] = useState('');
  const [title, setTitle] = useState('');
  const [description, setDescription] = useState('');
  const [customFields, setCustomFields] = useState<Record<string, string>>({
    urgency: 'Medium',
    justification: ''
  });
  const [error, setError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);

  // Fetch only active workflows for selection
  const { data: workflows = [], isLoading } = useQuery({
    queryKey: ['activeWorkflows'],
    queryFn: () => workflowService.getWorkflows(true)
  });

  const selectedWorkflow = workflows.find(w => w.id === workflowId);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);

    if (!workflowId) {
      setError('Please select an active workflow.');
      return;
    }

    if (!title.trim()) {
      setError('Please provide a title for your request.');
      return;
    }

    setSubmitting(true);
    try {
      const created = await requestService.createRequest({
        workflowId,
        title,
        description,
        data: customFields
      });
      navigate(`/requests/${created.id}`);
    } catch (err: any) {
      setError(err.response?.data?.message || 'Failed to submit request.');
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <div className="space-y-6 max-w-3xl mx-auto">
      {/* Header */}
      <div className="flex items-center gap-4">
        <Link
          to="/requests"
          className="p-2 glass-card hover:bg-slate-800 text-slate-300 rounded-xl transition-all"
        >
          <ArrowLeft className="w-4 h-4" />
        </Link>
        <div>
          <h1 className="text-2xl font-bold text-white tracking-tight flex items-center gap-2">
            <FileText className="w-6 h-6 text-indigo-400" />
            <span>Submit New Approval Request</span>
          </h1>
          <p className="text-slate-400 text-xs">
            Initiate a structured organizational request routed through automated approval checkpoints.
          </p>
        </div>
      </div>

      {error && (
        <div className="p-4 bg-rose-500/10 border border-rose-500/30 rounded-2xl text-rose-300 text-xs flex items-center gap-2.5">
          <AlertCircle className="w-4 h-4 shrink-0 text-rose-400" />
          <span>{error}</span>
        </div>
      )}

      <form onSubmit={handleSubmit} className="space-y-6">
        <div className="glass-panel p-6 rounded-3xl border border-slate-800 space-y-5">
          {/* Workflow Picker */}
          <div>
            <label className="block text-xs font-semibold text-slate-300 mb-1.5">
              Select Approval Workflow
            </label>
            {isLoading ? (
              <div className="text-xs text-slate-500">Loading active workflows...</div>
            ) : workflows.length === 0 ? (
              <div className="p-3 bg-amber-500/10 border border-amber-500/20 rounded-xl text-amber-300 text-xs">
                No active workflows configured. Please contact an Administrator to configure one.
              </div>
            ) : (
              <select
                required
                value={workflowId}
                onChange={(e) => {
                  setWorkflowId(e.target.value);
                  const w = workflows.find(wf => wf.id === e.target.value);
                  if (w && !title) {
                    setTitle(`${w.name} - ${new Date().toLocaleDateString()}`);
                  }
                }}
                className="w-full bg-slate-900 border border-slate-700/80 rounded-xl px-3.5 py-2.5 text-sm text-white outline-none focus:border-indigo-500 transition-all cursor-pointer"
              >
                <option value="">-- Choose a workflow --</option>
                {workflows.map((w) => (
                  <option key={w.id} value={w.id}>
                    {w.name} ({w.requestType}) — {w.steps.length} Approval Steps
                  </option>
                ))}
              </select>
            )}
          </div>

          {/* Workflow Flow Visual Preview */}
          {selectedWorkflow && (
            <div className="p-4 bg-indigo-950/20 border border-indigo-500/20 rounded-2xl space-y-2">
              <div className="flex items-center gap-2 text-indigo-300 text-xs font-bold">
                <ShieldCheck className="w-4 h-4" />
                <span>Configured Approval Path (v{selectedWorkflow.version})</span>
              </div>
              <p className="text-[11px] text-slate-400">{selectedWorkflow.description}</p>
              <div className="flex flex-wrap items-center gap-2 pt-2">
                {selectedWorkflow.steps.sort((a, b) => a.order - b.order).map((step, idx) => (
                  <React.Fragment key={step.name + idx}>
                    <div className="px-3 py-1.5 rounded-lg bg-slate-900 border border-indigo-500/30 text-xs text-slate-200 flex items-center gap-1.5">
                      <span className="w-4 h-4 rounded-full bg-indigo-600 text-[10px] font-bold text-white flex items-center justify-center">
                        {step.order}
                      </span>
                      <span>{step.name}</span>
                      <span className="text-[10px] text-indigo-400">({step.approverType})</span>
                    </div>
                    {idx < selectedWorkflow.steps.length - 1 && (
                      <ArrowRight className="w-3.5 h-3.5 text-slate-500" />
                    )}
                  </React.Fragment>
                ))}
              </div>
            </div>
          )}

          {/* Request Title */}
          <div>
            <label className="block text-xs font-semibold text-slate-300 mb-1.5">Request Title</label>
            <input
              type="text"
              required
              value={title}
              onChange={(e) => setTitle(e.target.value)}
              placeholder="e.g. Annual Vacation Leave Request"
              className="w-full bg-slate-900 border border-slate-700/80 rounded-xl px-3.5 py-2.5 text-sm text-white placeholder-slate-500 outline-none focus:border-indigo-500 transition-all"
            />
          </div>

          {/* Description */}
          <div>
            <label className="block text-xs font-semibold text-slate-300 mb-1.5">Description & Business Purpose</label>
            <textarea
              rows={3}
              required
              value={description}
              onChange={(e) => setDescription(e.target.value)}
              placeholder="Detail reasons, dates, items requested, or project justification..."
              className="w-full bg-slate-900 border border-slate-700/80 rounded-xl px-3.5 py-2.5 text-sm text-white placeholder-slate-500 outline-none focus:border-indigo-500 transition-all"
            />
          </div>

          {/* Custom Details */}
          <div className="grid md:grid-cols-2 gap-4 pt-2">
            <div>
              <label className="block text-xs font-semibold text-slate-300 mb-1.5">Urgency Level</label>
              <select
                value={customFields.urgency}
                onChange={(e) => setCustomFields({ ...customFields, urgency: e.target.value })}
                className="w-full bg-slate-900 border border-slate-700/80 rounded-xl px-3.5 py-2.5 text-xs text-white outline-none focus:border-indigo-500 transition-all"
              >
                <option value="Low">Low - Normal processing</option>
                <option value="Medium">Medium - Standard turnaround</option>
                <option value="High">High - Urgent review required</option>
              </select>
            </div>

            <div>
              <label className="block text-xs font-semibold text-slate-300 mb-1.5">Reference Note / Code</label>
              <input
                type="text"
                value={customFields.justification || ''}
                onChange={(e) => setCustomFields({ ...customFields, justification: e.target.value })}
                placeholder="Optional project code or reference ID"
                className="w-full bg-slate-900 border border-slate-700/80 rounded-xl px-3.5 py-2.5 text-xs text-white placeholder-slate-500 outline-none focus:border-indigo-500 transition-all"
              />
            </div>
          </div>
        </div>

        {/* Action Bar */}
        <div className="flex items-center justify-end gap-3">
          <Link
            to="/requests"
            className="px-5 py-2.5 glass-card hover:bg-slate-800 text-slate-300 rounded-xl text-xs font-semibold transition-all"
          >
            Cancel
          </Link>
          <button
            type="submit"
            disabled={submitting || !workflowId}
            className="flex items-center gap-2 px-6 py-2.5 bg-gradient-to-r from-indigo-600 to-indigo-500 hover:from-indigo-500 hover:to-indigo-400 text-white rounded-xl text-xs font-semibold shadow-lg shadow-indigo-600/30 transition-all disabled:opacity-50"
          >
            <Send className="w-4 h-4" />
            <span>{submitting ? 'Submitting...' : 'Submit Request'}</span>
          </button>
        </div>
      </form>
    </div>
  );
};
