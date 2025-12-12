using CampusMarketplace.Api.Data;
using CampusMarketplace.Api.Models;
using CampusMarketplace.Api.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CampusMarketplace.Api.Repositories;

// Repository for review operations with methods to find reviews by user
public class ReviewRepository : GenericRepository<Review>, IReviewRepository
{
    private readonly AppDbContext _ctx;

    public ReviewRepository(AppDbContext ctx) : base(ctx)
    {
        _ctx = ctx;
    }

    // Get all reviews received by a user
    public async Task<IEnumerable<Review>> GetForUserAsync(string userId)
    {
        return await _ctx.Reviews
            .Where(r => r.RevieweeId == userId)
            .Include(r => r.Reviewer)
            .ToListAsync();
    }

    // Get all reviews written by a user
    public async Task<IEnumerable<Review>> GetByReviewerAsync(string reviewerId)
    {
        return await _ctx.Reviews
            .Where(r => r.ReviewerId == reviewerId)
            .Include(r => r.Reviewee)
            .ToListAsync();
    }
}
