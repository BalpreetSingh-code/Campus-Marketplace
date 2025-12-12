using CampusMarketplace.Api.Data;
using CampusMarketplace.Api.Models;
using CampusMarketplace.Api.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CampusMarketplace.Api.Repositories;

// Repository for offer operations with methods to find offers by listing or buyer
public class OfferRepository : GenericRepository<Offer>, IOfferRepository
{
    private readonly AppDbContext _ctx;

    public OfferRepository(AppDbContext ctx) : base(ctx)
    {
        _ctx = ctx;
    }

    // Get all offers for a specific listing
    public async Task<IEnumerable<Offer>> GetByListingAsync(int listingId)
    {
        return await _ctx.Offers
            .Where(o => o.ListingId == listingId)
            .Include(o => o.Buyer)
            .ToListAsync();
    }

    // Get all offers made by a specific buyer
    public async Task<IEnumerable<Offer>> GetByBuyerAsync(string buyerId)
    {
        return await _ctx.Offers
            .Where(o => o.BuyerId == buyerId)
            .Include(o => o.Listing)
            .ToListAsync();
    }
}
