using CampusMarketplace.Api.Models;

namespace CampusMarketplace.Api.Repositories.Interfaces;

//
// ICategoryRepository — defines methods for working with categories
// Categories are like folders for organizing books (e.g., "Science", "Mathematics", "Literature")
// This interface extends IGenericRepository, so it has all the basic CRUD operations
// Plus it adds a special method to get a category along with all its listings
//
public interface ICategoryRepository : IGenericRepository<Category>
{
    //
    // GetCategoryWithListingsAsync — Get a category AND all the listings in that category
    // Example: GetCategoryWithListingsAsync(1) would return the "Science" category
    // along with all the science books that are listed for sale
    //
    Task<Category?> GetCategoryWithListingsAsync(int id);
}
