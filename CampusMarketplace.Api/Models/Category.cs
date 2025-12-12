namespace CampusMarketplace.Api.Models;

// Represents a book category for organizing listings
public class Category
{
    public int Id { get; set; }
    public string Name { get; set; } = default!;
    public string? Description { get; set; }

    public ICollection<Listing> Listings { get; set; } = new List<Listing>();
}
