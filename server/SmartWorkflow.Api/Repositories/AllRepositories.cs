using MongoDB.Driver;
using SmartWorkflow.Api.Database;
using SmartWorkflow.Api.Models;

namespace SmartWorkflow.Api.Repositories;

public interface IUserRepository : IRepository<User>
{
    Task<User?> GetByEmailAsync(string email);
}

public class UserRepository : MongoRepository<User>, IUserRepository
{
    public UserRepository(IMongoDbContext context) : base(context, "users") { }

    public async Task<User?> GetByEmailAsync(string email)
    {
        if (_context.IsConnected && _collection != null)
        {
            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
                var user = await _collection.Find(u => u.Email.ToLower() == email.ToLower()).FirstOrDefaultAsync(cts.Token);
                if (user != null) return user;
            }
            catch { /* fallback */ }
        }

        return MemoryStore.Values.FirstOrDefault(u => u.Email.Equals(email, StringComparison.OrdinalIgnoreCase));
    }
}

public interface IRoleRepository : IRepository<Role>
{
    Task<Role?> GetByNameAsync(string name);
}

public class RoleRepository : MongoRepository<Role>, IRoleRepository
{
    public RoleRepository(IMongoDbContext context) : base(context, "roles") { }

    public async Task<Role?> GetByNameAsync(string name)
    {
        if (_context.IsConnected && _collection != null)
        {
            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
                var role = await _collection.Find(r => r.Name == name).FirstOrDefaultAsync(cts.Token);
                if (role != null) return role;
            }
            catch { /* fallback */ }
        }

        return MemoryStore.Values.FirstOrDefault(r => r.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
    }
}

public interface IWorkflowRepository : IRepository<Workflow>
{
    Task<Workflow?> GetActiveByRequestTypeAsync(string requestType);
}

public class WorkflowRepository : MongoRepository<Workflow>, IWorkflowRepository
{
    public WorkflowRepository(IMongoDbContext context) : base(context, "workflows") { }

    public async Task<Workflow?> GetActiveByRequestTypeAsync(string requestType)
    {
        if (_context.IsConnected && _collection != null)
        {
            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
                var wf = await _collection.Find(w => w.RequestType == requestType && w.IsActive).FirstOrDefaultAsync(cts.Token);
                if (wf != null) return wf;
            }
            catch { /* fallback */ }
        }

        return MemoryStore.Values.FirstOrDefault(w => w.RequestType.Equals(requestType, StringComparison.OrdinalIgnoreCase) && w.IsActive);
    }
}

public interface IRequestRepository : IRepository<Request>
{
}

public class RequestRepository : MongoRepository<Request>, IRequestRepository
{
    public RequestRepository(IMongoDbContext context) : base(context, "requests") { }
}

public interface IApprovalRepository : IRepository<Approval>
{
    Task<List<Approval>> GetByRequestIdAsync(string requestId);
    Task<Approval?> GetCurrentPendingApprovalAsync(string requestId, int stepOrder);
}

public class ApprovalRepository : MongoRepository<Approval>, IApprovalRepository
{
    public ApprovalRepository(IMongoDbContext context) : base(context, "approvals") { }

    public async Task<List<Approval>> GetByRequestIdAsync(string requestId)
    {
        if (_context.IsConnected && _collection != null)
        {
            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
                return await _collection.Find(a => a.RequestId == requestId).SortBy(a => a.StepOrder).ToListAsync(cts.Token);
            }
            catch { /* fallback */ }
        }

        return MemoryStore.Values.Where(a => a.RequestId == requestId).OrderBy(a => a.StepOrder).ToList();
    }

    public async Task<Approval?> GetCurrentPendingApprovalAsync(string requestId, int stepOrder)
    {
        if (_context.IsConnected && _collection != null)
        {
            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
                return await _collection.Find(a => a.RequestId == requestId && a.StepOrder == stepOrder && a.Status == ApprovalStatus.Pending).FirstOrDefaultAsync(cts.Token);
            }
            catch { /* fallback */ }
        }

        return MemoryStore.Values.FirstOrDefault(a => a.RequestId == requestId && a.StepOrder == stepOrder && a.Status == ApprovalStatus.Pending);
    }
}

public interface ICommentRepository : IRepository<Comment>
{
    Task<List<Comment>> GetByRequestIdAsync(string requestId);
}

public class CommentRepository : MongoRepository<Comment>, ICommentRepository
{
    public CommentRepository(IMongoDbContext context) : base(context, "comments") { }

    public async Task<List<Comment>> GetByRequestIdAsync(string requestId)
    {
        if (_context.IsConnected && _collection != null)
        {
            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
                return await _collection.Find(c => c.RequestId == requestId).SortBy(c => c.CreatedAt).ToListAsync(cts.Token);
            }
            catch { /* fallback */ }
        }

        return MemoryStore.Values.Where(c => c.RequestId == requestId).OrderBy(c => c.CreatedAt).ToList();
    }
}

public interface INotificationRepository : IRepository<Notification>
{
    Task<List<Notification>> GetByUserIdAsync(string userId, bool unreadOnly = false);
    Task MarkAllAsReadAsync(string userId);
}

public class NotificationRepository : MongoRepository<Notification>, INotificationRepository
{
    public NotificationRepository(IMongoDbContext context) : base(context, "notifications") { }

    public async Task<List<Notification>> GetByUserIdAsync(string userId, bool unreadOnly = false)
    {
        if (_context.IsConnected && _collection != null)
        {
            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
                var filter = unreadOnly 
                    ? Builders<Notification>.Filter.Where(n => n.UserId == userId && !n.IsRead)
                    : Builders<Notification>.Filter.Where(n => n.UserId == userId);

                return await _collection.Find(filter).SortByDescending(n => n.CreatedAt).ToListAsync(cts.Token);
            }
            catch { /* fallback */ }
        }

        return MemoryStore.Values
            .Where(n => n.UserId == userId && (!unreadOnly || !n.IsRead))
            .OrderByDescending(n => n.CreatedAt)
            .ToList();
    }

    public async Task MarkAllAsReadAsync(string userId)
    {
        if (_context.IsConnected && _collection != null)
        {
            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
                var update = Builders<Notification>.Update.Set(n => n.IsRead, true);
                await _collection.UpdateManyAsync(n => n.UserId == userId, update, cancellationToken: cts.Token);
            }
            catch { /* fallback */ }
        }

        foreach (var n in MemoryStore.Values.Where(n => n.UserId == userId))
        {
            n.IsRead = true;
        }
    }
}

public interface IAuditLogRepository : IRepository<AuditLog>
{
    Task<List<AuditLog>> GetByRequestIdAsync(string requestId);
}

public class AuditLogRepository : MongoRepository<AuditLog>, IAuditLogRepository
{
    public AuditLogRepository(IMongoDbContext context) : base(context, "auditLogs") { }

    public async Task<List<AuditLog>> GetByRequestIdAsync(string requestId)
    {
        if (_context.IsConnected && _collection != null)
        {
            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
                return await _collection.Find(a => a.RequestId == requestId).SortByDescending(a => a.Timestamp).ToListAsync(cts.Token);
            }
            catch { /* fallback */ }
        }

        return MemoryStore.Values
            .Where(a => a.RequestId == requestId)
            .OrderByDescending(a => a.Timestamp)
            .ToList();
    }
}
