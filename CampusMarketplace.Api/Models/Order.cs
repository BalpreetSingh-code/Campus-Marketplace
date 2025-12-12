namespace CampusMarketplace.Api.Models;

// Represents a confirmed purchase order between buyer and seller
public class Order
{
    public int Id { get; set; }
    public string BuyerId { get; set; } = default!;
    public AppUser Buyer { get; set; } = default!;
    public int ListingId { get; set; }
    public Listing Listing { get; set; } = default!;
    public DateTime OrderDate { get; set; } = DateTime.UtcNow;
    public string Status { get; set; } = "Pending";
    public Review? Review { get; set; }
}
