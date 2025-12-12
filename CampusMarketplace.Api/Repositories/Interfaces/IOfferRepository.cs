using CampusMarketplace.Api.Models;

namespace CampusMarketplace.Api.Repositories.Interfaces;

//
// IOfferRepository — defines methods for working with offers
// Offers are price proposals that buyers make to sellers (e.g., "I'll pay $35 for this $40 book")
// This interface extends IGenericRepository, so it has all the basic CRUD operations
// Plus it adds methods to find offers by listing or by buyer
//
public interface IOfferRepository : IGenericRepository<Offer>
{
    //
    // GetByListingAsync — Get all offers made on a specific listing
    // Example: GetByListingAsync(5) returns all offers made on listing #5
    // This is useful for sellers to see all the offers they've received for one of their books
    //
    Task<IEnumerable<Offer>> GetByListingAsync(int listingId);
    
    //
    // GetByBuyerAsync — Get all offers made by a specific buyer
    // Example: GetByBuyerAsync("user123") returns all offers that user123 has made
    // This is useful for buyers to see all their pending offers in one place
    //
    Task<IEnumerable<Offer>> GetByBuyerAsync(string buyerId);
}
