import React, { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { Link } from 'react-router-dom';
import { 
  GitFork, 
  Plus, 
  CheckCircle2, 
  XCircle, 
  Edit3, 
  Trash2, 
  ArrowRight,
  Layers,
  Search
} from 'lucide-react';
import { workflowService } from '../services';

export const WorkflowsPage: React.FC = () => {
  const [search, setSearch] = useState('');
  const queryClient = useQueryClient();

  const { data: workflows = [], isLoading } = useQuery({
    queryKey: ['workflows'],
    queryFn: () => workflowService.getWorkflows()
  });

  const toggleActiveMutation = useMutation({
    mutationFn: ({ id, isActive }: { id: string; isActive: boolean }) => 
      isActive ? workflowService.deactivate(id) : workflowService.activate(id),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['workflows'] })
  });

  const deleteMutation = useMutation({
    mutationFn: (id: string) => workflowService.deleteWorkflow(id),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['workflows'] })
  });

  const filtered = workflows.filter(w => 
    w.name.toLowerCase().includes(search.toLowerCase()) || 
    w.requestType.toLowerCase().includes(search.toLowerCase()) ||
    w.description.toLowerCase().includes(search.toLowerCase())
  );

  return (
    <div className="space-y-6 max-w-7xl mx-auto">
      {/* Header */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div>
          <h1 className="text-2xl font-bold text-white tracking-tight flex items-center gap-2.5">
            <GitFork className="w-6 h-6 text-indigo-400" />
            <span>Dynamic Workflow Builder</span>
          </h1>
          <p className="text-slate-400 text-sm">
            Design, version, and manage database-driven approval pipelines without changing backend code.
          </p>
        </div>

        <Link
          to="/workflows/create"
          className="inline-flex items-center gap-2 px-4 py-2.5 bg-gradient-to-r from-indigo-600 to-indigo-500 hover:from-indigo-500 hover:to-indigo-400 text-white rounded-xl text-sm font-semibold shadow-lg shadow-indigo-600/25 transition-all"
        >
          <Plus className="w-4 h-4" />
          <span>New Workflow</span>
        </Link>
      </div>

      {/* Filter and Search Bar */}
      <div className="glass-panel p-4 rounded-2xl flex items-center gap-3">
        <Search className="w-4 h-4 text-slate-400 ml-1" />
        <input
          type="text"
          value={search}
          onChange={(e) => setSearch(e.target.value)}
          placeholder="Search workflows by title, type, or description..."
          className="w-full bg-transparent text-sm text-white placeholder-slate-500 outline-none"
        />
      </div>

      {/* Workflows List */}
      {isLoading ? (
        <div className="text-center py-12 text-slate-400 text-sm">Loading workflows...</div>
      ) : filtered.length === 0 ? (
        <div className="glass-panel p-12 rounded-3xl text-center space-y-4">
          <div className="w-12 h-12 rounded-full bg-indigo-500/10 text-indigo-400 flex items-center justify-center mx-auto">
            <Layers className="w-6 h-6" />
          </div>
          <h3 className="text-lg font-bold text-white">No Workflows Found</h3>
          <p className="text-slate-400 text-xs max-w-md mx-auto">
            Create your first dynamic workflow definition or click "Seed Sample Workflows" in the sidebar to populate defaults.
          </p>
          <Link
            to="/workflows/create"
            className="inline-flex items-center gap-2 px-4 py-2 bg-indigo-600 text-white text-xs font-semibold rounded-xl"
          >
            <Plus className="w-3.5 h-3.5" />
            <span>Create Workflow</span>
          </Link>
        </div>
      ) : (
        <div className="grid md:grid-cols-2 lg:grid-cols-3 gap-6">
          {filtered.map((w) => (
            <div key={w.id} className="glass-panel p-6 rounded-3xl border border-slate-800 flex flex-col justify-between space-y-5 hover:border-slate-700 transition-all">
              <div>
                <div className="flex items-start justify-between gap-3 mb-2">
                  <h3 className="text-base font-bold text-white">{w.name}</h3>
                  <span className={`px-2 py-0.5 rounded-full text-[10px] font-bold ${
                    w.isActive ? 'bg-emerald-500/20 text-emerald-300 border border-emerald-500/30' : 'bg-slate-700 text-slate-400'
                  }`}>
                    {w.isActive ? 'Active' : 'Inactive'}
                  </span>
                </div>
                <p className="text-xs text-slate-400 line-clamp-2 mb-4">{w.description || 'No description provided.'}</p>

                <div className="space-y-2 py-3 border-y border-slate-800/80">
                  <div className="flex justify-between text-xs">
                    <span className="text-slate-400">Request Type:</span>
                    <span className="text-indigo-300 font-semibold">{w.requestType}</span>
                  </div>
                  <div className="flex justify-between text-xs">
                    <span className="text-slate-400">Version:</span>
                    <span className="text-slate-200 font-medium">v{w.version}</span>
                  </div>
                  <div className="flex justify-between text-xs">
                    <span className="text-slate-400">Approval Steps:</span>
                    <span className="text-slate-200 font-medium">{w.steps.length} steps</span>
                  </div>
                </div>

                {/* Steps Mini Flow */}
                <div className="pt-3">
                  <span className="text-[11px] font-semibold text-slate-400 block mb-2">Sequential Flow:</span>
                  <div className="flex flex-wrap items-center gap-1.5">
                    {w.steps.sort((a, b) => a.order - b.order).map((step, idx) => (
                      <React.Fragment key={step.name + idx}>
                        <span className="text-[10px] px-2 py-1 bg-slate-800/80 text-slate-300 rounded-lg border border-slate-700/60 font-medium">
                          {step.order}. {step.name} ({step.approverType === 'Role' ? step.approverRole : step.approverType})
                        </span>
                        {idx < w.steps.length - 1 && (
                          <ArrowRight className="w-3 h-3 text-slate-500" />
                        )}
                      </React.Fragment>
                    ))}
                  </div>
                </div>
              </div>

              {/* Action Buttons */}
              <div className="flex items-center justify-between pt-4 border-t border-slate-800">
                <button
                  type="button"
                  onClick={() => toggleActiveMutation.mutate({ id: w.id, isActive: w.isActive })}
                  className={`text-xs font-semibold flex items-center gap-1.5 transition-colors ${
                    w.isActive ? 'text-amber-400 hover:text-amber-300' : 'text-emerald-400 hover:text-emerald-300'
                  }`}
                >
                  {w.isActive ? <XCircle className="w-4 h-4" /> : <CheckCircle2 className="w-4 h-4" />}
                  <span>{w.isActive ? 'Deactivate' : 'Activate'}</span>
                </button>

                <div className="flex items-center gap-2">
                  <Link
                    to={`/workflows/${w.id}/edit`}
                    className="p-2 text-slate-400 hover:text-indigo-400 hover:bg-indigo-500/10 rounded-lg transition-all"
                    title="Edit Workflow Steps"
                  >
                    <Edit3 className="w-4 h-4" />
                  </Link>
                  <button
                    type="button"
                    onClick={() => {
                      if (confirm(`Are you sure you want to deactivate workflow '${w.name}'?`)) {
                        deleteMutation.mutate(w.id);
                      }
                    }}
                    className="p-2 text-slate-400 hover:text-rose-400 hover:bg-rose-500/10 rounded-lg transition-all"
                    title="Delete Workflow"
                  >
                    <Trash2 className="w-4 h-4" />
                  </button>
                </div>
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  );
};
