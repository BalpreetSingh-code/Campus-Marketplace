using CampusMarketplace.Api.Models;

namespace CampusMarketplace.Api.Repositories.Interfaces;

//
// IReviewRepository — defines methods for working with reviews
// Reviews are feedback that buyers leave after completing a purchase (rating 1-5 stars + comment)
// They help build trust in the marketplace by letting users rate each other
// This interface extends IGenericRepository, so it has all the basic CRUD operations
// Plus it adds methods to find reviews received by a user or written by a user
//
public interface IReviewRepository : IGenericRepository<Review>
{
    //
    // GetForUserAsync — Get all reviews that a user has received (reviews about them)
    // Example: GetForUserAsync("user123") returns all reviews that other users wrote about user123
    // This is useful for displaying a user's reputation/rating on their profile
    //
    Task<IEnumerable<Review>> GetForUserAsync(string userId);
    
    //
    // GetByReviewerAsync — Get all reviews that a user has written (reviews they wrote)
    // Example: GetByReviewerAsync("user123") returns all reviews that user123 wrote about others
    // This is useful for users to see their own review history
    //
    Task<IEnumerable<Review>> GetByReviewerAsync(string reviewerId);
}
