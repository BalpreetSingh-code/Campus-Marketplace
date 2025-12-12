using Microsoft.AspNetCore.Identity;

namespace CampusMarketplace.Api.Models;

// User model extending Identity with profile information
public class AppUser : IdentityUser
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? ProfilePictureUrl { get; set; }

    // User's listings and reviews
    public ICollection<Listing> Listings { get; set; } = new List<Listing>();
    public ICollection<Review> Reviews { get; set; } = new List<Review>();
}