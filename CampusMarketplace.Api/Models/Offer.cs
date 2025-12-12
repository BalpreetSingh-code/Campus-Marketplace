namespace CampusMarketplace.Api.Models;

// Represents a buyer's price offer on a listing
public class Offer
{
    public int Id { get; set; }
    public string BuyerId { get; set; } = default!;
    public AppUser Buyer { get; set; } = default!;
    public int ListingId { get; set; }
    public Listing Listing { get; set; } = default!;
    public decimal OfferedPrice { get; set; }
    public string Status { get; set; } = "Pending";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
