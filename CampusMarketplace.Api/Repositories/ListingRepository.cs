using CampusMarketplace.Api.Data;
using CampusMarketplace.Api.Models;
using CampusMarketplace.Api.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CampusMarketplace.Api.Repositories;

// Repository for listing operations with methods to load related category and seller data
public class ListingRepository : GenericRepository<Listing>, IListingRepository
{
    private readonly AppDbContext _ctx;

    public ListingRepository(AppDbContext ctx) : base(ctx)
    {
        _ctx = ctx;
    }

    // Get all listings with category and seller information loaded
    public async Task<IEnumerable<Listing>> GetAllWithDetailsAsync()
    {
        return await _ctx.Listings
            .Include(l => l.Category)
            .Include(l => l.Seller)
            .ToListAsync();
    }

    // Get all listings for a specific seller
    public async Task<IEnumerable<Listing>> GetBySellerAsync(string sellerId)
    {
        return await _ctx.Listings
            .Where(l => l.SellerId == sellerId)
            .Include(l => l.Category)
            .ToListAsync();
    }

    // Get paginated listings with sorting, filtering, and search
    // Only returns listings that are not sold
    public async Task<PaginatedList<Listing>> GetPagedAsync(
        string? sortOrder = null,
        string? searchString = null,
        int pageNumber = 1,
        int pageSize = 10)
    {
        var query = _ctx.Listings
            .Where(l => !l.IsSold)
            .Include(l => l.Category)
            .Include(l => l.Seller)
            .AsQueryable();

        // Apply search filter
        if (!string.IsNullOrEmpty(searchString))
        {
            query = query.Where(l =>
                l.Title.Contains(searchString) ||
                l.Description.Contains(searchString) ||
                (l.Category != null && l.Category.Name.Contains(searchString)));
        }

        // Apply sorting
        query = sortOrder switch
        {
            "title_desc" => query.OrderByDescending(l => l.Title),
            "title" => query.OrderBy(l => l.Title),
            "price_desc" => query.OrderByDescending(l => l.Price),
            "price" => query.OrderBy(l => l.Price),
            "category_desc" => query.OrderByDescending(l => l.Category != null ? l.Category.Name : ""),
            "category" => query.OrderBy(l => l.Category != null ? l.Category.Name : ""),
            "condition_desc" => query.OrderByDescending(l => l.Condition),
            "condition" => query.OrderBy(l => l.Condition),
            _ => query.OrderBy(l => l.Title)
        };

        return await PaginatedList<Listing>.CreateAsync(query.AsNoTracking(), pageNumber, pageSize);
    }
}
