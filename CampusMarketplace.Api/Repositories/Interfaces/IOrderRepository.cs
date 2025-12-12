using CampusMarketplace.Api.Models;

namespace CampusMarketplace.Api.Repositories.Interfaces;

//
// IOrderRepository — defines methods for working with orders
// Orders represent confirmed purchases between buyers and sellers
// An order is created when a buyer decides to purchase a listing (either directly or from an accepted offer)
// This interface extends IGenericRepository, so it has all the basic CRUD operations
// Plus it adds methods to find orders by buyer or get an order with its review
//
public interface IOrderRepository : IGenericRepository<Order>
{
    //
    // GetByBuyerAsync — Get all orders placed by a specific buyer
    // Example: GetByBuyerAsync("user123") returns all orders that user123 has placed
    // This is useful for buyers to see their purchase history
    //
    Task<IEnumerable<Order>> GetByBuyerAsync(string buyerId);
    
    //
    // GetWithReviewAsync — Get an order along with the review (if one was left)
    // Example: GetWithReviewAsync(10) returns order #10 and the review that was left for it
    // This is useful when displaying order details - you can show if a review was already written
    //
    Task<Order?> GetWithReviewAsync(int orderId);
    
    //
    // GetBySellerAsync — Get all orders for listings created by a specific seller
    // Example: GetBySellerAsync("user123") returns all orders for listings that user123 is selling
    // This is useful for sellers to see purchase requests for their listings
    //
    Task<IEnumerable<Order>> GetBySellerAsync(string sellerId);
}
