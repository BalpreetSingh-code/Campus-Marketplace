using CampusMarketplace.Api.Models;

namespace CampusMarketplace.Api.Repositories.Interfaces;

//
// IListingRepository — defines methods for working with listings
// Listings are books that sellers post for sale
// This interface extends IGenericRepository, so it has all the basic CRUD operations
// Plus it adds special methods to get listings with related information
//
public interface IListingRepository : IGenericRepository<Listing>
{
    //
    // GetAllWithDetailsAsync — Get all listings along with their category and seller information
    // This is useful when displaying listings to buyers - they can see the category and who's selling
    // Example: Instead of just "Book Title $40", you'd get "Book Title $40 - Science Category - Sold by Alice"
    //
    Task<IEnumerable<Listing>> GetAllWithDetailsAsync();
    
    //
    // GetBySellerAsync — Get all listings created by a specific seller
    // Example: GetBySellerAsync("user123") returns all books that user123 is selling
    // This is useful for sellers to see all their listings, or for buyers to see all books from one seller
    //
    Task<IEnumerable<Listing>> GetBySellerAsync(string sellerId);

    //
    // GetPagedAsync — Get listings with sorting, filtering, and paging support
    // sortOrder: Column to sort by (e.g., "title", "title_desc", "price", "price_desc")
    // searchString: General search term to filter by title, description, or category name
    // pageNumber: Page number (1-based)
    // pageSize: Number of items per page
    // Returns a PaginatedList containing the paged results
    //
    Task<PaginatedList<Listing>> GetPagedAsync(
        string? sortOrder = null,
        string? searchString = null,
        int pageNumber = 1,
        int pageSize = 10);
}
