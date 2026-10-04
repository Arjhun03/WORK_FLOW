using MongoDB.Driver;
using SmartWorkflow.Api.Models;

namespace SmartWorkflow.Api.Database;

public interface IMongoDbContext
{
    IMongoClient? Client { get; }
    IMongoDatabase? Database { get; }
    IMongoCollection<User>? Users { get; }
    IMongoCollection<Role>? Roles { get; }
    IMongoCollection<Workflow>? Workflows { get; }
    IMongoCollection<Request>? Requests { get; }
    IMongoCollection<Approval>? Approvals { get; }
    IMongoCollection<Comment>? Comments { get; }
    IMongoCollection<Notification>? Notifications { get; }
    IMongoCollection<AuditLog>? AuditLogs { get; }
    bool IsConnected { get; }
    Task InitializeIndexesAsync();
    Task<bool> PingAsync();
}

public class MongoDbContext : IMongoDbContext
{
    private readonly IMongoClient? _client;
    private readonly IMongoDatabase? _database;
    private readonly ILogger<MongoDbContext> _logger;
    private bool _isConnected = false;

    public MongoDbContext(IConfiguration configuration, ILogger<MongoDbContext> logger)
    {
        _logger = logger;

        var connectionString = Environment.GetEnvironmentVariable("MONGODB_CONNECTION_STRING")
                               ?? configuration["MongoDb:ConnectionString"];

        var databaseName = Environment.GetEnvironmentVariable("MONGODB_DATABASE_NAME")
                           ?? configuration["MongoDb:DatabaseName"]
                           ?? "SmartWorkflowDb";

        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            try
            {
                var settings = MongoClientSettings.FromConnectionString(connectionString);
                settings.ServerSelectionTimeout = TimeSpan.FromSeconds(15);
                _client = new MongoClient(settings);
                _database = _client.GetDatabase(databaseName);
                _isConnected = true; // Database configured and ready to execute
            }
            catch (Exception ex)
            {
                _logger.LogWarning("MongoDB client initialization error: {Message}", ex.Message);
            }
        }
    }

    public IMongoClient? Client => _client;
    public IMongoDatabase? Database => _database;
    public bool IsConnected => _database != null && _isConnected;

    public IMongoCollection<User>? Users => _database?.GetCollection<User>("users");
    public IMongoCollection<Role>? Roles => _database?.GetCollection<Role>("roles");
    public IMongoCollection<Workflow>? Workflows => _database?.GetCollection<Workflow>("workflows");
    public IMongoCollection<Request>? Requests => _database?.GetCollection<Request>("requests");
    public IMongoCollection<Approval>? Approvals => _database?.GetCollection<Approval>("approvals");
    public IMongoCollection<Comment>? Comments => _database?.GetCollection<Comment>("comments");
    public IMongoCollection<Notification>? Notifications => _database?.GetCollection<Notification>("notifications");
    public IMongoCollection<AuditLog>? AuditLogs => _database?.GetCollection<AuditLog>("auditLogs");

    public async Task<bool> PingAsync()
    {
        if (_database == null)
        {
            _isConnected = false;
            return false;
        }

        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            await _database.RunCommandAsync((Command<MongoDB.Bson.BsonDocument>)"{ping:1}", cancellationToken: cts.Token);
            _isConnected = true;
            _logger.LogInformation("Successfully connected to MongoDB database.");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning("MongoDB ping warning ({Message}). Database operations will still attempt with connection pool retries.", ex.Message);
            return false;
        }
    }

    public async Task InitializeIndexesAsync()
    {
        if (!IsConnected || _database == null) return;

        try
        {
            var userIndex = new CreateIndexModel<User>(
                Builders<User>.IndexKeys.Ascending(u => u.Email),
                new CreateIndexOptions { Unique = true, Sparse = true });
            if (Users != null) await Users.Indexes.CreateOneAsync(userIndex);

            if (Requests != null)
            {
                await Requests.Indexes.CreateManyAsync(new[]
                {
                    new CreateIndexModel<Request>(Builders<Request>.IndexKeys.Ascending(r => r.RequestedBy)),
                    new CreateIndexModel<Request>(Builders<Request>.IndexKeys.Ascending(r => r.Status)),
                    new CreateIndexModel<Request>(Builders<Request>.IndexKeys.Ascending(r => r.WorkflowId)),
                    new CreateIndexModel<Request>(Builders<Request>.IndexKeys.Ascending(r => r.CurrentApproverId))
                });
            }

            if (Approvals != null)
            {
                await Approvals.Indexes.CreateManyAsync(new[]
                {
                    new CreateIndexModel<Approval>(Builders<Approval>.IndexKeys.Ascending(a => a.ApproverId)),
                    new CreateIndexModel<Approval>(Builders<Approval>.IndexKeys.Ascending(a => a.Status)),
                    new CreateIndexModel<Approval>(Builders<Approval>.IndexKeys.Ascending(a => a.RequestId))
                });
            }

            if (Notifications != null)
            {
                await Notifications.Indexes.CreateOneAsync(
                    new CreateIndexModel<Notification>(Builders<Notification>.IndexKeys.Ascending(n => n.UserId)));
            }

            if (AuditLogs != null)
            {
                await AuditLogs.Indexes.CreateOneAsync(
                    new CreateIndexModel<AuditLog>(Builders<AuditLog>.IndexKeys.Ascending(a => a.RequestId)));
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning("MongoDB index initialization: {Message}", ex.Message);
        }
    }
}
