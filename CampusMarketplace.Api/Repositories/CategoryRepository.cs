using CampusMarketplace.Api.Data;
using CampusMarketplace.Api.Models;
using CampusMarketplace.Api.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CampusMarketplace.Api.Repositories;

// Repository for category operations with method to load related listings
public class CategoryRepository : GenericRepository<Category>, ICategoryRepository
{
    private readonly AppDbContext _ctx;

    public CategoryRepository(AppDbContext ctx) : base(ctx)
    {
        _ctx = ctx;
    }

    // Get category with all its listings loaded
    public async Task<Category?> GetCategoryWithListingsAsync(int id)
    {
        return await _ctx.Categories
            .Include(c => c.Listings)
            .FirstOrDefaultAsync(c => c.Id == id);
    }
}
