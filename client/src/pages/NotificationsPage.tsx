import React from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { Link } from 'react-router-dom';
import { 
  Bell, 
  CheckCheck, 
  Clock, 
  CheckCircle2, 
  XCircle, 
  MessageSquare,
  ArrowRight
} from 'lucide-react';
import { notificationService } from '../services';

export const NotificationsPage: React.FC = () => {
  const queryClient = useQueryClient();

  const { data: notifications = [], isLoading } = useQuery({
    queryKey: ['allNotifications'],
    queryFn: () => notificationService.getNotifications(false),
    refetchInterval: 8000
  });

  const markReadMutation = useMutation({
    mutationFn: (id: string) => notificationService.markAsRead(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['allNotifications'] });
      queryClient.invalidateQueries({ queryKey: ['notificationsCount'] });
    }
  });

  const markAllReadMutation = useMutation({
    mutationFn: () => notificationService.markAllAsRead(),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['allNotifications'] });
      queryClient.invalidateQueries({ queryKey: ['notificationsCount'] });
    }
  });

  const getNotificationIcon = (type: string) => {
    switch (type) {
      case 'ApprovalRequired':
        return <Clock className="w-4 h-4 text-amber-400" />;
      case 'RequestApproved':
      case 'RequestCompleted':
        return <CheckCircle2 className="w-4 h-4 text-emerald-400" />;
      case 'RequestRejected':
        return <XCircle className="w-4 h-4 text-rose-400" />;
      case 'CommentAdded':
        return <MessageSquare className="w-4 h-4 text-indigo-400" />;
      default:
        return <Bell className="w-4 h-4 text-indigo-400" />;
    }
  };

  return (
    <div className="space-y-6 max-w-4xl mx-auto">
      {/* Header */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div>
          <h1 className="text-2xl font-bold text-white tracking-tight flex items-center gap-2.5">
            <Bell className="w-6 h-6 text-indigo-400" />
            <span>Activity Notifications</span>
          </h1>
          <p className="text-slate-400 text-sm">
            Live updates on assigned approval tasks, status transitions, and discussion notes.
          </p>
        </div>

        {notifications.some(n => !n.isRead) && (
          <button
            type="button"
            onClick={() => markAllReadMutation.mutate()}
            disabled={markAllReadMutation.isPending}
            className="flex items-center gap-1.5 px-4 py-2 glass-card hover:bg-slate-800 text-indigo-300 rounded-xl text-xs font-semibold transition-all"
          >
            <CheckCheck className="w-4 h-4" />
            <span>Mark All as Read</span>
          </button>
        )}
      </div>

      {/* Notifications List */}
      <div className="glass-panel rounded-3xl border border-slate-800 p-6 space-y-3">
        {isLoading ? (
          <div className="py-12 text-center text-slate-400 text-xs">Loading notifications...</div>
        ) : notifications.length === 0 ? (
          <div className="py-12 text-center text-slate-500 text-xs">
            No notifications received yet. You're completely up to date!
          </div>
        ) : (
          notifications.map((n) => (
            <div
              key={n.id}
              className={`p-4 rounded-2xl border transition-all flex items-start justify-between gap-4 ${
                !n.isRead
                  ? 'bg-indigo-950/20 border-indigo-500/30'
                  : 'bg-slate-900/40 border-slate-800/80 hover:bg-slate-900/60'
              }`}
            >
              <div className="flex items-start gap-3">
                <div className="p-2 rounded-xl bg-slate-900 border border-slate-800 shrink-0">
                  {getNotificationIcon(n.type)}
                </div>
                <div className="space-y-1">
                  <p className="text-xs text-white leading-relaxed">{n.message}</p>
                  <span className="text-[10px] text-slate-500">
                    {new Date(n.createdAt).toLocaleString()}
                  </span>
                </div>
              </div>

              <div className="flex items-center gap-2 shrink-0">
                {n.requestId && (
                  <Link
                    to={`/requests/${n.requestId}`}
                    onClick={() => { if (!n.isRead) markReadMutation.mutate(n.id); }}
                    className="flex items-center gap-1 px-3 py-1.5 bg-slate-800 hover:bg-slate-700 text-slate-300 hover:text-white rounded-lg text-xs font-semibold transition-all"
                  >
                    <span>View</span>
                    <ArrowRight className="w-3 h-3" />
                  </Link>
                )}
                {!n.isRead && (
                  <button
                    type="button"
                    onClick={() => markReadMutation.mutate(n.id)}
                    className="text-[10px] font-semibold text-indigo-400 hover:text-indigo-300 px-2 py-1"
                  >
                    Mark read
                  </button>
                )}
              </div>
            </div>
          ))
        )}
      </div>
    </div>
  );
};
