using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace CampusMarketplace.Api.Repositories;

//
// IGenericRepository.cs — defines common data access methods for any entity
// This is a contract (interface) that says "any repository must have these basic operations"
// A Repository is like a helper that talks to the database for us, so we don't have to write SQL directly
// This interface can work with any type of entity (Category, Listing, Offer, etc.)
//
public interface IGenericRepository<T> where T : class
{
    //
    // GetAsync — Find one specific item by its ID
    // Example: GetAsync(5) would find the item with ID = 5
    // Returns null if the item doesn't exist
    //
    Task<T?> GetAsync(int id);

    //
    // GetAllAsync — Get all items, or filter to find specific ones
    // If you don't provide a filter, it returns everything
    // If you provide a filter, it only returns items that match
    // Example: GetAllAsync(x => x.Price > 50) would return only items with price greater than 50
    //
    Task<IEnumerable<T>> GetAllAsync(Expression<Func<T, bool>>? filter = null);

    //
    // AddAsync — Add a new item to the database
    // Example: AddAsync(new Category { Name = "Science" }) adds a new category
    // Note: This adds it to memory, you still need to call SaveAsync() to actually save it
    //
    Task AddAsync(T entity);

    //
    // Update — Mark an existing item as changed so it will be updated in the database
    // Example: Change a listing's price, then call Update() to save that change
    // Note: You still need to call SaveAsync() to actually save it
    //
    void Update(T entity);

    //
    // Remove — Mark an item for deletion
    // Example: Remove(listing) marks that listing to be deleted
    // Note: You still need to call SaveAsync() to actually delete it
    //
    void Remove(T entity);
}
