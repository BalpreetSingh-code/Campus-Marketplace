namespace CampusMarketplace.Api.Models;

// Represents a book listing posted by a seller
public class Listing
{
    public int Id { get; set; }
    public string Title { get; set; } = default!;
    public string Description { get; set; } = default!;
    public decimal Price { get; set; }
    public string Condition { get; set; } = "Good";
    public bool IsSold { get; set; } = false;

    // Category relationship
    public int CategoryId { get; set; }
    public Category? Category { get; set; }

    // Seller relationship
    public string SellerId { get; set; } = default!;
    public AppUser? Seller { get; set; }

    // Related collections
    public ICollection<Offer> Offers { get; set; } = new List<Offer>();
    public ICollection<Order> Orders { get; set; } = new List<Order>();
}
