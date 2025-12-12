using CampusMarketplace.Api.Data;
using CampusMarketplace.Api.Models;
using CampusMarketplace.Api.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CampusMarketplace.Api.Repositories;

// Repository for user operations with method to load related listings and reviews
public class UserRepository : GenericRepository<AppUser>, IUserRepository
{
    private readonly AppDbContext _ctx;

    public UserRepository(AppDbContext ctx) : base(ctx)
    {
        _ctx = ctx;
    }

    // Get user with their listings and reviews loaded
    public async Task<AppUser?> GetUserWithDetailsAsync(string userId)
    {
        return await _ctx.Users
            .Include(u => u.Listings)
            .Include(u => u.Reviews)
            .FirstOrDefaultAsync(u => u.Id == userId);
    }
}
