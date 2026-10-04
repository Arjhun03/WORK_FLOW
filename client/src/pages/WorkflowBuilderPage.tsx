import React, { useState, useEffect } from 'react';
import { useNavigate, useParams, Link } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import { 
  GitFork, 
  Plus, 
  Trash2, 
  ArrowUp, 
  ArrowDown, 
  Save, 
  ArrowLeft,
  AlertCircle
} from 'lucide-react';
import { workflowService, userService } from '../services';
import type { WorkflowStep, ApproverType } from '../types';

export const WorkflowBuilderPage: React.FC = () => {
  const { id } = useParams<{ id: string }>();
  const isEditing = Boolean(id);
  const navigate = useNavigate();

  const [name, setName] = useState('');
  const [description, setDescription] = useState('');
  const [requestType, setRequestType] = useState('Leave');
  const [isActive, setIsActive] = useState(true);
  const [steps, setSteps] = useState<WorkflowStep[]>([
    {
      name: 'Manager Approval',
      order: 1,
      approverType: 'Manager',
      isRequired: true
    }
  ]);
  const [error, setError] = useState<string | null>(null);
  const [saving, setSaving] = useState(false);

  // Fetch available users and roles for step configuration
  const { data: users = [] } = useQuery({
    queryKey: ['workflowUsersList'],
    queryFn: () => userService.getUsers()
  });

  const { data: roles = [] } = useQuery({
    queryKey: ['workflowRolesList'],
    queryFn: () => userService.getRoles()
  });

  // Fetch existing workflow if editing
  const { data: existingWorkflow } = useQuery({
    queryKey: ['workflow', id],
    queryFn: () => workflowService.getWorkflowById(id!),
    enabled: isEditing
  });

  useEffect(() => {
    if (existingWorkflow) {
      setName(existingWorkflow.name);
      setDescription(existingWorkflow.description);
      setRequestType(existingWorkflow.requestType);
      setIsActive(existingWorkflow.isActive);
      if (existingWorkflow.steps && existingWorkflow.steps.length > 0) {
        setSteps(existingWorkflow.steps.sort((a, b) => a.order - b.order));
      }
    }
  }, [existingWorkflow]);

  const addStep = () => {
    const newStep: WorkflowStep = {
      name: `Step ${steps.length + 1} Approval`,
      order: steps.length + 1,
      approverType: 'Role',
      approverRole: 'HR',
      isRequired: true
    };
    setSteps([...steps, newStep]);
  };

  const removeStep = (index: number) => {
    if (steps.length <= 1) {
      setError('A workflow must have at least one approval step.');
      return;
    }
    const updated = steps.filter((_, i) => i !== index).map((s, idx) => ({ ...s, order: idx + 1 }));
    setSteps(updated);
  };

  const moveStep = (index: number, direction: 'up' | 'down') => {
    if (direction === 'up' && index === 0) return;
    if (direction === 'down' && index === steps.length - 1) return;

    const newSteps = [...steps];
    const targetIndex = direction === 'up' ? index - 1 : index + 1;
    const temp = newSteps[index];
    newSteps[index] = newSteps[targetIndex];
    newSteps[targetIndex] = temp;

    setSteps(newSteps.map((s, idx) => ({ ...s, order: idx + 1 })));
  };

  const updateStep = (index: number, field: keyof WorkflowStep, value: any) => {
    const newSteps = [...steps];
    newSteps[index] = { ...newSteps[index], [field]: value };
    setSteps(newSteps);
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);

    if (!name.trim()) {
      setError('Please provide a workflow name.');
      return;
    }

    if (steps.length === 0) {
      setError('At least one approval step is required.');
      return;
    }

    setSaving(true);
    try {
      if (isEditing && id) {
        await workflowService.updateWorkflow(id, {
          name,
          description,
          requestType,
          isActive,
          steps
        });
      } else {
        await workflowService.createWorkflow({
          name,
          description,
          requestType,
          steps
        });
      }
      navigate('/workflows');
    } catch (err: any) {
      setError(err.response?.data?.message || 'Failed to save workflow definition.');
    } finally {
      setSaving(false);
    }
  };

  return (
    <div className="space-y-6 max-w-4xl mx-auto">
      {/* Header */}
      <div className="flex items-center gap-4">
        <Link
          to="/workflows"
          className="p-2 glass-card hover:bg-slate-800 text-slate-300 rounded-xl transition-all"
        >
          <ArrowLeft className="w-4 h-4" />
        </Link>
        <div>
          <h1 className="text-2xl font-bold text-white tracking-tight flex items-center gap-2">
            <GitFork className="w-6 h-6 text-indigo-400" />
            <span>{isEditing ? 'Edit Workflow Pipeline' : 'Design Dynamic Workflow'}</span>
          </h1>
          <p className="text-slate-400 text-xs">
            Configure step sequences, approval rules, and role assignments stored directly in MongoDB.
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
        {/* Basic Information */}
        <div className="glass-panel p-6 rounded-3xl border border-slate-800 space-y-4">
          <h2 className="text-sm font-bold text-white uppercase tracking-wider text-indigo-400">
            1. Workflow Information
          </h2>

          <div className="grid md:grid-cols-2 gap-4">
            <div>
              <label className="block text-xs font-semibold text-slate-300 mb-1.5">Workflow Name</label>
              <input
                type="text"
                required
                value={name}
                onChange={(e) => setName(e.target.value)}
                placeholder="e.g. Employee Leave Request"
                className="w-full bg-slate-900 border border-slate-700/80 rounded-xl px-3.5 py-2.5 text-sm text-white placeholder-slate-500 outline-none focus:border-indigo-500 transition-all"
              />
            </div>

            <div>
              <label className="block text-xs font-semibold text-slate-300 mb-1.5">Request Type Identifier</label>
              <input
                type="text"
                required
                value={requestType}
                onChange={(e) => setRequestType(e.target.value)}
                placeholder="e.g. Leave, Equipment, Expense"
                className="w-full bg-slate-900 border border-slate-700/80 rounded-xl px-3.5 py-2.5 text-sm text-white placeholder-slate-500 outline-none focus:border-indigo-500 transition-all"
              />
            </div>
          </div>

          <div>
            <label className="block text-xs font-semibold text-slate-300 mb-1.5">Description</label>
            <textarea
              rows={2}
              value={description}
              onChange={(e) => setDescription(e.target.value)}
              placeholder="Describe what requests this workflow handles and any compliance rules..."
              className="w-full bg-slate-900 border border-slate-700/80 rounded-xl px-3.5 py-2.5 text-sm text-white placeholder-slate-500 outline-none focus:border-indigo-500 transition-all"
            />
          </div>
        </div>

        {/* Dynamic Approval Steps Builder */}
        <div className="glass-panel p-6 rounded-3xl border border-slate-800 space-y-4">
          <div className="flex items-center justify-between">
            <div>
              <h2 className="text-sm font-bold text-white uppercase tracking-wider text-indigo-400">
                2. Approval Steps Flow
              </h2>
              <p className="text-xs text-slate-400">Steps will execute sequentially from 1 to {steps.length}.</p>
            </div>
            <button
              type="button"
              onClick={addStep}
              className="flex items-center gap-1.5 px-3 py-1.5 bg-indigo-600/30 hover:bg-indigo-600/50 text-indigo-300 border border-indigo-500/40 rounded-xl text-xs font-semibold transition-all"
            >
              <Plus className="w-3.5 h-3.5" />
              <span>Add Step</span>
            </button>
          </div>

          <div className="space-y-4">
            {steps.map((step, index) => (
              <div 
                key={step.stepId || `step-${index}`}
                className="p-4 bg-slate-900/80 rounded-2xl border border-slate-800 space-y-4 relative"
              >
                <div className="flex items-center justify-between gap-3 border-b border-slate-800 pb-3">
                  <div className="flex items-center gap-2.5">
                    <span className="w-6 h-6 rounded-full bg-indigo-600 text-white text-xs font-bold flex items-center justify-center">
                      {step.order}
                    </span>
                    <span className="text-xs font-bold text-white">Step {step.order}</span>
                  </div>

                  <div className="flex items-center gap-1">
                    <button
                      type="button"
                      disabled={index === 0}
                      onClick={() => moveStep(index, 'up')}
                      className="p-1.5 text-slate-400 hover:text-white disabled:opacity-30 rounded"
                      title="Move Step Up"
                    >
                      <ArrowUp className="w-4 h-4" />
                    </button>
                    <button
                      type="button"
                      disabled={index === steps.length - 1}
                      onClick={() => moveStep(index, 'down')}
                      className="p-1.5 text-slate-400 hover:text-white disabled:opacity-30 rounded"
                      title="Move Step Down"
                    >
                      <ArrowDown className="w-4 h-4" />
                    </button>
                    <button
                      type="button"
                      onClick={() => removeStep(index)}
                      className="p-1.5 text-slate-400 hover:text-rose-400 rounded"
                      title="Delete Step"
                    >
                      <Trash2 className="w-4 h-4" />
                    </button>
                  </div>
                </div>

                <div className="grid md:grid-cols-2 gap-4">
                  <div>
                    <label className="block text-[11px] font-semibold text-slate-400 mb-1">Step Name</label>
                    <input
                      type="text"
                      required
                      value={step.name}
                      onChange={(e) => updateStep(index, 'name', e.target.value)}
                      placeholder="e.g. Line Manager Approval"
                      className="w-full bg-slate-950 border border-slate-800 rounded-xl px-3 py-2 text-xs text-white outline-none focus:border-indigo-500"
                    />
                  </div>

                  <div>
                    <label className="block text-[11px] font-semibold text-slate-400 mb-1">Approver Strategy</label>
                    <select
                      value={step.approverType}
                      onChange={(e) => updateStep(index, 'approverType', e.target.value as ApproverType)}
                      className="w-full bg-slate-950 border border-slate-800 rounded-xl px-3 py-2 text-xs text-white outline-none focus:border-indigo-500"
                    >
                      <option value="Manager">Manager (Requester's Direct Line Manager)</option>
                      <option value="Role">Role (Any active user with designated role)</option>
                      <option value="User">Specific User (Exact individual)</option>
                      <option value="DepartmentRole">Department Role (Requester's Department)</option>
                    </select>
                  </div>
                </div>

                {/* Conditional Fields based on Approver Strategy */}
                {step.approverType === 'Role' && (
                  <div>
                    <label className="block text-[11px] font-semibold text-slate-400 mb-1">Select Approver Role</label>
                    <select
                      value={step.approverRole || ''}
                      onChange={(e) => updateStep(index, 'approverRole', e.target.value)}
                      className="w-full bg-slate-950 border border-slate-800 rounded-xl px-3 py-2 text-xs text-white outline-none focus:border-indigo-500"
                    >
                      <option value="">-- Choose Role --</option>
                      {roles.map((r) => (
                        <option key={r.name} value={r.name}>{r.name} ({r.description})</option>
                      ))}
                    </select>
                  </div>
                )}

                {step.approverType === 'User' && (
                  <div>
                    <label className="block text-[11px] font-semibold text-slate-400 mb-1">Select Specific User</label>
                    <select
                      value={step.approverUserId || ''}
                      onChange={(e) => updateStep(index, 'approverUserId', e.target.value)}
                      className="w-full bg-slate-950 border border-slate-800 rounded-xl px-3 py-2 text-xs text-white outline-none focus:border-indigo-500"
                    >
                      <option value="">-- Choose User --</option>
                      {users.map((u) => (
                        <option key={u.id} value={u.id}>{u.name} ({u.email} - {u.roles.join(', ')})</option>
                      ))}
                    </select>
                  </div>
                )}

                {step.approverType === 'DepartmentRole' && (
                  <div className="grid md:grid-cols-2 gap-4">
                    <div>
                      <label className="block text-[11px] font-semibold text-slate-400 mb-1">Department Role</label>
                      <select
                        value={step.approverRole || ''}
                        onChange={(e) => updateStep(index, 'approverRole', e.target.value)}
                        className="w-full bg-slate-950 border border-slate-800 rounded-xl px-3 py-2 text-xs text-white outline-none focus:border-indigo-500"
                      >
                        <option value="">-- Choose Role --</option>
                        {roles.map((r) => (
                          <option key={r.name} value={r.name}>{r.name}</option>
                        ))}
                      </select>
                    </div>
                  </div>
                )}
              </div>
            ))}
          </div>
        </div>

        {/* Submit Bar */}
        <div className="flex items-center justify-end gap-3 pt-2">
          <Link
            to="/workflows"
            className="px-5 py-2.5 glass-card hover:bg-slate-800 text-slate-300 rounded-xl text-xs font-semibold transition-all"
          >
            Cancel
          </Link>
          <button
            type="submit"
            disabled={saving}
            className="flex items-center gap-2 px-6 py-2.5 bg-gradient-to-r from-indigo-600 to-indigo-500 hover:from-indigo-500 hover:to-indigo-400 text-white rounded-xl text-xs font-semibold shadow-lg shadow-indigo-600/30 transition-all disabled:opacity-50"
          >
            <Save className="w-4 h-4" />
            <span>{saving ? 'Saving Workflow...' : isEditing ? 'Update Workflow' : 'Save Workflow'}</span>
          </button>
        </div>
      </form>
    </div>
  );
};
