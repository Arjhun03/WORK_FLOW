import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { Sparkles, ArrowRight, Lock, Mail, AlertCircle } from 'lucide-react';
import { useAuth } from '../context/AuthContext';

export const LoginPage: React.FC = () => {
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);
  const { login } = useAuth();
  const navigate = useNavigate();

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);
    setLoading(true);
    try {
      await login(email, password);
      navigate('/dashboard');
    } catch (err: any) {
      setError(err.response?.data?.message || 'Invalid email or password.');
    } finally {
      setLoading(false);
    }
  };

  const handleQuickLogin = async (quickEmail: string) => {
    setEmail(quickEmail);
    setPassword('password123');
    setError(null);
    setLoading(true);
    try {
      await login(quickEmail, 'password123');
      navigate('/dashboard');
    } catch (err: any) {
      setError(err.response?.data?.message || 'Demo login failed. Make sure database is seeded.');
    } finally {
      setLoading(false);
    }
  };

  const demoAccounts = [
    { role: 'Admin', email: 'admin@example.com', desc: 'Manage workflows & users', badge: 'bg-purple-500/20 text-purple-300' },
    { role: 'Manager', email: 'manager@example.com', desc: 'First-line approver', badge: 'bg-blue-500/20 text-blue-300' },
    { role: 'Employee', email: 'employee@example.com', desc: 'Submits requests', badge: 'bg-emerald-500/20 text-emerald-300' },
    { role: 'HR', email: 'hr@example.com', desc: 'HR step approver', badge: 'bg-pink-500/20 text-pink-300' },
    { role: 'IT', email: 'it@example.com', desc: 'Equipment reviewer', badge: 'bg-amber-500/20 text-amber-300' },
    { role: 'Finance', email: 'finance@example.com', desc: 'Budget sign-off', badge: 'bg-teal-500/20 text-teal-300' },
  ];

  return (
    <div className="min-h-screen bg-[#0b0f19] flex items-center justify-center p-4 selection:bg-indigo-500 selection:text-white">
      <div className="w-full max-w-4xl grid md:grid-cols-2 gap-8 items-center">
        {/* Left Column: Branding & Quick Role Sign-in */}
        <div className="space-y-6">
          <div className="flex items-center gap-3">
            <div className="w-12 h-12 rounded-2xl bg-gradient-to-tr from-indigo-600 via-indigo-500 to-purple-500 flex items-center justify-center shadow-xl shadow-indigo-500/25 text-white">
              <Sparkles className="w-6 h-6" />
            </div>
            <div>
              <h1 className="text-2xl font-extrabold text-white tracking-tight">SmartWorkflow</h1>
              <p className="text-xs text-indigo-400 font-semibold tracking-wider uppercase">Dynamic Approval System</p>
            </div>
          </div>

          <div>
            <h2 className="text-2xl font-bold text-white mb-2">Automate & Streamline Organizational Approvals</h2>
            <p className="text-slate-400 text-sm leading-relaxed">
              Database-driven workflows, multi-step routing, real-time audit trails, and role-based authorization in one unified platform.
            </p>
          </div>

          <div className="space-y-3 pt-2">
            <p className="text-xs font-bold text-slate-400 uppercase tracking-wider">Quick Demo Login (Password: password123)</p>
            <div className="grid grid-cols-2 gap-2">
              {demoAccounts.map((acc) => (
                <button
                  key={acc.email}
                  type="button"
                  onClick={() => handleQuickLogin(acc.email)}
                  className="p-3 glass-card rounded-xl text-left hover:border-indigo-500/50 group transition-all"
                >
                  <div className="flex items-center justify-between mb-1">
                    <span className="text-xs font-bold text-white group-hover:text-indigo-400 transition-colors">{acc.role}</span>
                    <span className={`text-[10px] px-1.5 py-0.5 rounded-md font-medium ${acc.badge}`}>{acc.role}</span>
                  </div>
                  <p className="text-[11px] text-slate-400 truncate">{acc.email}</p>
                </button>
              ))}
            </div>
          </div>
        </div>

        {/* Right Column: Sign In Card */}
        <div className="glass-panel p-8 rounded-3xl shadow-2xl border border-slate-800/80">
          <h2 className="text-xl font-bold text-white mb-1">Sign In to Your Account</h2>
          <p className="text-slate-400 text-xs mb-6">Enter your credentials to access your dashboard</p>

          {error && (
            <div className="mb-5 p-3.5 bg-rose-500/10 border border-rose-500/30 rounded-xl text-rose-300 text-xs flex items-center gap-2.5">
              <AlertCircle className="w-4 h-4 shrink-0 text-rose-400" />
              <span>{error}</span>
            </div>
          )}

          <form onSubmit={handleSubmit} className="space-y-4">
            <div>
              <label className="block text-xs font-semibold text-slate-300 mb-1.5">Email Address</label>
              <div className="relative">
                <Mail className="w-4 h-4 text-slate-500 absolute left-3.5 top-1/2 -translate-y-1/2" />
                <input
                  type="email"
                  required
                  value={email}
                  onChange={(e) => setEmail(e.target.value)}
                  placeholder="admin@example.com"
                  className="w-full bg-slate-900/80 border border-slate-700/80 focus:border-indigo-500 focus:ring-1 focus:ring-indigo-500 rounded-xl pl-10 pr-4 py-2.5 text-sm text-white placeholder-slate-500 outline-none transition-all"
                />
              </div>
            </div>

            <div>
              <label className="block text-xs font-semibold text-slate-300 mb-1.5">Password</label>
              <div className="relative">
                <Lock className="w-4 h-4 text-slate-500 absolute left-3.5 top-1/2 -translate-y-1/2" />
                <input
                  type="password"
                  required
                  value={password}
                  onChange={(e) => setPassword(e.target.value)}
                  placeholder="••••••••"
                  className="w-full bg-slate-900/80 border border-slate-700/80 focus:border-indigo-500 focus:ring-1 focus:ring-indigo-500 rounded-xl pl-10 pr-4 py-2.5 text-sm text-white placeholder-slate-500 outline-none transition-all"
                />
              </div>
            </div>

            <button
              type="submit"
              disabled={loading}
              className="w-full mt-2 py-3 bg-gradient-to-r from-indigo-600 to-indigo-500 hover:from-indigo-500 hover:to-indigo-400 text-white rounded-xl text-sm font-semibold shadow-lg shadow-indigo-600/30 flex items-center justify-center gap-2 transition-all disabled:opacity-50"
            >
              {loading ? (
                <div className="w-5 h-5 border-2 border-white border-t-transparent rounded-full animate-spin" />
              ) : (
                <>
                  <span>Sign In</span>
                  <ArrowRight className="w-4 h-4" />
                </>
              )}
            </button>
          </form>
        </div>
      </div>
    </div>
  );
};
