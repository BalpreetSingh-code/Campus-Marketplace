using CampusMarketplace.Api.Data;
using CampusMarketplace.Api.Models;
using CampusMarketplace.Api.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CampusMarketplace.Api.Repositories;

// Repository for order operations with methods to find orders by buyer or seller
public class OrderRepository : GenericRepository<Order>, IOrderRepository
{
    private readonly AppDbContext _ctx;

    public OrderRepository(AppDbContext ctx) : base(ctx)
    {
        _ctx = ctx;
    }

    // Get all orders placed by a specific buyer
    public async Task<IEnumerable<Order>> GetByBuyerAsync(string buyerId)
    {
        return await _ctx.Orders
            .Where(o => o.BuyerId == buyerId)
            .Include(o => o.Listing)
            .ThenInclude(l => l.Category)
            .ToListAsync();
    }

    // Get order with its review (if one exists)
    public async Task<Order?> GetWithReviewAsync(int orderId)
    {
        return await _ctx.Orders
            .Include(o => o.Review)
            .FirstOrDefaultAsync(o => o.Id == orderId);
    }
    
    // Get all orders for listings created by a specific seller
    public async Task<IEnumerable<Order>> GetBySellerAsync(string sellerId)
    {
        return await _ctx.Orders
            .Include(o => o.Listing)
            .ThenInclude(l => l.Category)
            .Include(o => o.Buyer)
            .Where(o => o.Listing != null && o.Listing.SellerId == sellerId)
            .ToListAsync();
    }
}
