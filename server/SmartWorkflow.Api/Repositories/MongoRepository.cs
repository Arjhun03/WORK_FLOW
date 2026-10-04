using System.Collections.Concurrent;
using System.Linq.Expressions;
using MongoDB.Bson;
using MongoDB.Driver;
using SmartWorkflow.Api.Database;
using SmartWorkflow.Api.Models;

namespace SmartWorkflow.Api.Repositories;

public class MongoRepository<T> : IRepository<T> where T : BaseEntity
{
    protected readonly IMongoDbContext _context;
    protected readonly string _collectionName;
    protected readonly IMongoCollection<T>? _collection;

    private static readonly ConcurrentDictionary<string, ConcurrentDictionary<string, T>> _memoryStores = new();

    protected ConcurrentDictionary<string, T> MemoryStore =>
        _memoryStores.GetOrAdd(typeof(T).Name, _ => new ConcurrentDictionary<string, T>());

    public MongoRepository(IMongoDbContext context, string collectionName)
    {
        _context = context;
        _collectionName = collectionName;
        _collection = context.Database?.GetCollection<T>(collectionName);
    }

    public virtual async Task<List<T>> GetAllAsync(Expression<Func<T, bool>>? filter = null)
    {
        if (_collection != null)
        {
            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
                var query = filter == null ? _collection.Find(_ => true) : _collection.Find(filter);
                var list = await query.ToListAsync(cts.Token);
                // Sync to memory store
                foreach (var item in list)
                {
                    MemoryStore[item.Id] = item;
                }
                return list;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MONGO GETALL WARNING] in {_collectionName}: {ex.Message}");
            }
        }

        var items = MemoryStore.Values.AsQueryable();
        if (filter != null)
        {
            items = items.Where(filter);
        }
        return await Task.FromResult(items.ToList());
    }

    public virtual async Task<T?> GetByIdAsync(string id)
    {
        if (_collection != null)
        {
            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
                var item = await _collection.Find(x => x.Id == id).FirstOrDefaultAsync(cts.Token);
                if (item != null)
                {
                    MemoryStore[id] = item;
                    return item;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MONGO GETBYID WARNING] in {_collectionName}: {ex.Message}");
            }
        }

        MemoryStore.TryGetValue(id, out var entity);
        return await Task.FromResult(entity);
    }

    public virtual async Task<T?> FindOneAsync(Expression<Func<T, bool>> filter)
    {
        if (_collection != null)
        {
            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
                var item = await _collection.Find(filter).FirstOrDefaultAsync(cts.Token);
                if (item != null)
                {
                    MemoryStore[item.Id] = item;
                    return item;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MONGO FINDONE WARNING] in {_collectionName}: {ex.Message}");
            }
        }

        var compiled = filter.Compile();
        var localItem = MemoryStore.Values.FirstOrDefault(compiled);
        return await Task.FromResult(localItem);
    }

    public virtual async Task<List<T>> FindAsync(Expression<Func<T, bool>> filter)
    {
        if (_collection != null)
        {
            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
                var list = await _collection.Find(filter).ToListAsync(cts.Token);
                foreach (var item in list)
                {
                    MemoryStore[item.Id] = item;
                }
                return list;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MONGO FIND WARNING] in {_collectionName}: {ex.Message}");
            }
        }

        var compiled = filter.Compile();
        var items = MemoryStore.Values.Where(compiled).ToList();
        return await Task.FromResult(items);
    }

    public virtual async Task<T> CreateAsync(T entity)
    {
        if (string.IsNullOrWhiteSpace(entity.Id))
        {
            entity.Id = ObjectId.GenerateNewId().ToString();
        }
        entity.CreatedAt = DateTime.UtcNow;

        if (_collection != null)
        {
            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
                await _collection.InsertOneAsync(entity, cancellationToken: cts.Token);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MONGO CREATE ERROR] in {_collectionName}: {ex}");
            }
        }

        MemoryStore[entity.Id] = entity;
        return entity;
    }

    public virtual async Task<bool> UpdateAsync(string id, T entity)
    {
        entity.Id = id;
        entity.UpdatedAt = DateTime.UtcNow;

        if (_collection != null)
        {
            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
                var result = await _collection.ReplaceOneAsync(
                    x => x.Id == id, 
                    entity, 
                    new ReplaceOptions { IsUpsert = true }, 
                    cancellationToken: cts.Token);

                MemoryStore[id] = entity;
                return result.IsAcknowledged;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MONGO UPDATE ERROR] in {_collectionName}: {ex}");
            }
        }

        MemoryStore[id] = entity;
        return await Task.FromResult(true);
    }

    public virtual async Task<bool> DeleteAsync(string id)
    {
        if (_collection != null)
        {
            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
                var result = await _collection.DeleteOneAsync(x => x.Id == id, cancellationToken: cts.Token);
                MemoryStore.TryRemove(id, out _);
                return result.IsAcknowledged && result.DeletedCount > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MONGO DELETE WARNING] in {_collectionName}: {ex.Message}");
            }
        }

        return await Task.FromResult(MemoryStore.TryRemove(id, out _));
    }

    public virtual async Task<long> CountAsync(Expression<Func<T, bool>>? filter = null)
    {
        if (_collection != null)
        {
            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
                return filter == null 
                    ? await _collection.CountDocumentsAsync(_ => true, cancellationToken: cts.Token)
                    : await _collection.CountDocumentsAsync(filter, cancellationToken: cts.Token);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MONGO COUNT WARNING] in {_collectionName}: {ex.Message}");
            }
        }

        if (filter == null)
        {
            return await Task.FromResult((long)MemoryStore.Count);
        }

        var compiled = filter.Compile();
        return await Task.FromResult((long)MemoryStore.Values.Count(compiled));
    }

    public virtual async Task<(List<T> Items, long TotalCount)> GetPagedAsync(
        int page, 
        int pageSize, 
        Expression<Func<T, bool>>? filter = null,
        Expression<Func<T, object>>? orderBy = null,
        bool isDescending = true)
    {
        if (_collection != null)
        {
            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
                var findFluent = filter == null ? _collection.Find(_ => true) : _collection.Find(filter);
                var totalCount = await (filter == null 
                    ? _collection.CountDocumentsAsync(_ => true, cancellationToken: cts.Token) 
                    : _collection.CountDocumentsAsync(filter, cancellationToken: cts.Token));

                findFluent = orderBy != null
                    ? (isDescending ? findFluent.SortByDescending(orderBy) : findFluent.SortBy(orderBy))
                    : findFluent.SortByDescending(x => x.CreatedAt);

                var items = await findFluent
                    .Skip((page - 1) * pageSize)
                    .Limit(pageSize)
                    .ToListAsync(cts.Token);

                foreach (var item in items)
                {
                    MemoryStore[item.Id] = item;
                }

                return (items, totalCount);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MONGO GETPAGED WARNING] in {_collectionName}: {ex.Message}");
            }
        }

        var query = MemoryStore.Values.AsQueryable();
        if (filter != null)
        {
            query = query.Where(filter);
        }

        var total = query.Count();
        var pagedItems = query
            .OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return await Task.FromResult((pagedItems, (long)total));
    }
}
