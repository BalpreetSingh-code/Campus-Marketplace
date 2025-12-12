using CampusMarketplace.Api.Models;

namespace CampusMarketplace.Api.Repositories.Interfaces;

//
// IUserRepository — defines methods for working with users
// Users are the people using the marketplace (buyers, sellers, admins)
// This interface extends IGenericRepository, so it has all the basic CRUD operations
// Plus it adds a method to get a user along with their related information
//
public interface IUserRepository : IGenericRepository<AppUser>
{
    //
    // GetUserWithDetailsAsync — Get a user along with their listings and reviews
    // Example: GetUserWithDetailsAsync("user123") returns user123's info plus all their listings and reviews
    // This is useful for displaying a user's profile page with all their activity
    //
    Task<AppUser?> GetUserWithDetailsAsync(string userId);
}
