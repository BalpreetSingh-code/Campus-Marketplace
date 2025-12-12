using CampusMarketplace.Api.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace CampusMarketplace.Api.Repositories;

// Generic repository providing basic CRUD operations for any entity type
public class GenericRepository<T> : IGenericRepository<T> where T : class
{
    private readonly AppDbContext _ctx;
    private readonly DbSet<T> _set;

    public GenericRepository(AppDbContext ctx)
    {
        _ctx = ctx;
        _set = _ctx.Set<T>();
    }

    // Get one item by ID
    public async Task<T?> GetAsync(int id) => await _set.FindAsync(id);

    // Get all items, optionally filtered
    public async Task<IEnumerable<T>> GetAllAsync(Expression<Func<T, bool>>? filter = null)
        => filter is null
            ? await _set.ToListAsync()
            : await _set.Where(filter).ToListAsync();

    // Add new item (call SaveAsync to persist)
    public async Task AddAsync(T entity) => await _set.AddAsync(entity);

    // Update existing item (call SaveAsync to persist)
    public void Update(T entity) => _set.Update(entity);

    // Delete item (call SaveAsync to persist)
    public void Remove(T entity) => _set.Remove(entity);
}
