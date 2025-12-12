using CampusMarketplace.Api.Models;
using CampusMarketplace.Api.Repositories.Interfaces;

namespace CampusMarketplace.Api.Repositories;

//
// IUnitOfWork.cs — defines a single access point for all repositories
// Think of Unit of Work as a manager that coordinates all our database operations
// Instead of using repositories separately, we use UnitOfWork which gives us access to all of them
// This ensures all changes are saved together (all succeed or all fail - like a transaction)
//
public interface IUnitOfWork
{
    //
    // These are the repositories - each one handles a specific type of data
    // Categories repository — for managing book categories (Science, Math, etc.)
    //
    ICategoryRepository Categories { get; }
    
    // Listings repository — for managing book listings (books for sale)
    IListingRepository Listings { get; }
    
    // Offers repository — for managing offers that buyers make to sellers
    IOfferRepository Offers { get; }
    
    // Orders repository — for managing orders (confirmed purchases)
    IOrderRepository Orders { get; }
    
    // Reviews repository — for managing reviews that users leave after transactions
    IReviewRepository Reviews { get; }
    
    // Users repository — for managing user information
    IUserRepository Users { get; }
    
    //
    // SaveAsync — Save all changes to the database
    // This is important: when you add, update, or remove items, they don't save automatically
    // You must call SaveAsync() to actually write them to the database
    // Example: You might add a listing, then add an offer, then call SaveAsync() once to save both
    //
    Task<int> SaveAsync();
}
