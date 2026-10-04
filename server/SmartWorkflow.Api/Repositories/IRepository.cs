using System.Linq.Expressions;
using SmartWorkflow.Api.Models;

namespace SmartWorkflow.Api.Repositories;

public interface IRepository<T> where T : BaseEntity
{
    Task<List<T>> GetAllAsync(Expression<Func<T, bool>>? filter = null);
    Task<T?> GetByIdAsync(string id);
    Task<T?> FindOneAsync(Expression<Func<T, bool>> filter);
    Task<List<T>> FindAsync(Expression<Func<T, bool>> filter);
    Task<T> CreateAsync(T entity);
    Task<bool> UpdateAsync(string id, T entity);
    Task<bool> DeleteAsync(string id);
    Task<long> CountAsync(Expression<Func<T, bool>>? filter = null);
    Task<(List<T> Items, long TotalCount)> GetPagedAsync(
        int page, 
        int pageSize, 
        Expression<Func<T, bool>>? filter = null,
        Expression<Func<T, object>>? orderBy = null,
        bool isDescending = true);
}
