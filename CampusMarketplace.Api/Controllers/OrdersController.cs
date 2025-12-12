using CampusMarketplace.Api.Models;
using CampusMarketplace.Api.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using System.Security.Claims;

namespace CampusMarketplace.Api.Controllers;

// Handles order operations (buyers create orders, sellers accept/complete them)
[Route("[controller]")]
[Authorize]
public class OrdersController : Controller
{
    private readonly IUnitOfWork _uow;

    public OrdersController(IUnitOfWork uow) => _uow = uow;

    //
    // GET: /Orders or /Orders/Index
    // Returns all orders — Admin only
    //
    [Authorize(Roles = "Admin")]
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        try
        {
            Log.Information("Admin retrieving all orders");
            var orders = await _uow.Orders.GetAllAsync();
            Log.Information("Retrieved {Count} orders", orders.Count());
            return View(orders);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error retrieving orders");
            return StatusCode(500, "An error occurred while retrieving orders.");
        }
    }

    //
    // GET: /Orders/MyOrders
    // Returns all orders made by the current buyer
    //
    [Authorize(Roles = "Buyer")]
    [HttpGet("MyOrders")]
    public async Task<IActionResult> MyOrders()
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            Log.Information("User {UserId} retrieving their orders", userId);
            
            var orders = await _uow.Orders.GetByBuyerAsync(userId!);
            Log.Information("User {UserId} has {Count} orders", userId, orders.Count());
            return View(orders);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error retrieving user orders");
            return StatusCode(500, "An error occurred while retrieving your orders.");
        }
    }

    //
    // GET: /Orders/MySellerOrders
    // Returns all orders for listings created by the current seller
    //
    [Authorize(Roles = "Seller,Admin")]
    [HttpGet("MySellerOrders")]
    public async Task<IActionResult> MySellerOrders()
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = User.IsInRole("Admin");
            Log.Information("User {UserId} retrieving orders for their listings", userId);
            
            var orders = await _uow.Orders.GetBySellerAsync(userId!);
            Log.Information("User {UserId} has {Count} orders for their listings", userId, orders.Count());
            return View(orders);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error retrieving seller orders");
            return StatusCode(500, "An error occurred while retrieving your orders.");
        }
    }

    //
    // GET: /Orders/Details/{id}
    // Returns a single order by ID
    //
    [HttpGet("Details/{id}")]
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        try
        {
            Log.Information("Getting order with ID: {OrderId}", id);
            var order = await _uow.Orders.GetAsync(id.Value);
            
            if (order == null)
            {
                Log.Warning("Order with ID {OrderId} not found", id);
                return NotFound();
            }

            // Load related entities
            var listing = await _uow.Listings.GetAsync(order.ListingId);
            if (listing != null)
            {
                order.Listing = listing;
                var category = await _uow.Categories.GetAsync(listing.CategoryId);
                if (category != null)
                {
                    listing.Category = category;
                }
            }
            
            var orderWithReview = await _uow.Orders.GetWithReviewAsync(id.Value);
            if (orderWithReview != null)
            {
                order = orderWithReview;
            }

            if (order == null)
            {
                Log.Warning("Order with ID {OrderId} not found after loading review", id);
                return NotFound();
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = User.IsInRole("Admin");
            var isBuyer = order.BuyerId == userId;
            var isSeller = order.Listing != null && order.Listing.SellerId == userId;

            // Only allow buyer, seller, or admin to view the order
            if (!isAdmin && !isBuyer && !isSeller)
            {
                Log.Warning("User {UserId} attempted to view order {OrderId} they don't have access to", 
                    userId, id);
                return Forbid();
            }

            Log.Information("Successfully retrieved order {OrderId}", id);
            return View(order);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error retrieving order {OrderId}", id);
            return StatusCode(500, "An error occurred while retrieving the order.");
        }
    }

    //
    // GET: /Orders/Create
    // Shows the create form
    //
    [Authorize(Roles = "Buyer")]
    [HttpGet("Create")]
    public async Task<IActionResult> Create(int? listingId)
    {
        if (listingId == null)
        {
            return NotFound();
        }

        var listing = await _uow.Listings.GetAsync(listingId.Value);
        if (listing == null)
        {
            return NotFound();
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (listing.SellerId == userId)
        {
            return BadRequest("You cannot create an order for your own listing.");
        }

        // Prevent creating orders for sold listings
        if (listing.IsSold)
        {
            return BadRequest("This listing has already been sold and is no longer available.");
        }

        ViewData["Listing"] = listing;
        return View(new Order { ListingId = listingId.Value });
    }

    //
    // POST: /Orders/Create
    // Creates a new order — only allowed for Buyers
    //
    [Authorize(Roles = "Buyer")]
    [HttpPost("Create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Order order)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            Log.Information("User {UserId} creating order for listing {ListingId}", userId, order.ListingId);

            // Remove fields from validation that are set by the controller, not the form
            ModelState.Remove("BuyerId");
            ModelState.Remove("Buyer");
            ModelState.Remove("Listing");
            ModelState.Remove("Status");
            ModelState.Remove("OrderDate");

            if (!ModelState.IsValid)
            {
                Log.Warning("Invalid model state for order creation by user {UserId}", userId);
                return BadRequest(ModelState);
            }

            // Verify listing exists
            var listing = await _uow.Listings.GetAsync(order.ListingId);
            if (listing == null)
            {
                Log.Warning("Listing {ListingId} not found for order creation", order.ListingId);
                return NotFound($"Listing with ID {order.ListingId} not found.");
            }

            // Prevent buying your own listing
            if (listing.SellerId == userId)
            {
                Log.Warning("User {UserId} attempted to create order for their own listing {ListingId}", 
                    userId, order.ListingId);
                return BadRequest("You cannot create an order for your own listing.");
            }

            // Prevent creating orders for sold listings
            if (listing.IsSold)
            {
                Log.Warning("User {UserId} attempted to create order for sold listing {ListingId}", 
                    userId, order.ListingId);
                return BadRequest("This listing has already been sold and is no longer available.");
            }

            order.BuyerId = userId!;
            order.Status = "Pending";
            order.OrderDate = DateTime.UtcNow;

            await _uow.Orders.AddAsync(order);
            await _uow.SaveAsync();

            Log.Information("Order {OrderId} created successfully by user {UserId}", order.Id, userId);
            return RedirectToAction(nameof(MyOrders));
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error creating order");
            var listing = await _uow.Listings.GetAsync(order.ListingId);
            ViewData["Listing"] = listing;
            return View(order);
        }
    }

    //
    // POST: /Orders/CreateFromOffer/{offerId}
    // Creates an order from an accepted offer
    //
    [Authorize(Roles = "Buyer")]
    [HttpPost("CreateFromOffer/{offerId}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateFromOffer(int offerId)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            Log.Information("User {UserId} creating order from offer {OfferId}", userId, offerId);

            var offer = await _uow.Offers.GetAsync(offerId);
            if (offer == null)
            {
                Log.Warning("Offer {OfferId} not found", offerId);
                return NotFound($"Offer with ID {offerId} not found.");
            }

            // Verify offer belongs to user
            if (offer.BuyerId != userId)
            {
                Log.Warning("User {UserId} attempted to create order from offer {OfferId} they don't own", 
                    userId, offerId);
                return Forbid("You can only create orders from your own offers.");
            }

            // Verify offer is accepted
            if (offer.Status != "Accepted")
            {
                Log.Warning("Offer {OfferId} is not accepted (status: {Status})", offerId, offer.Status);
                return BadRequest("Only accepted offers can be converted to orders.");
            }

            // Check if order already exists for this offer
            var existingOrders = await _uow.Orders.GetByBuyerAsync(userId!);
            if (existingOrders.Any(o => o.ListingId == offer.ListingId && o.Status != "Cancelled"))
            {
                Log.Warning("Order already exists for listing {ListingId} by user {UserId}", 
                    offer.ListingId, userId);
                return BadRequest("An active order already exists for this listing.");
            }

            var order = new Order
            {
                BuyerId = userId!,
                ListingId = offer.ListingId,
                Status = "Pending",
                OrderDate = DateTime.UtcNow
            };

            await _uow.Orders.AddAsync(order);
            await _uow.SaveAsync();

            Log.Information("Order {OrderId} created from offer {OfferId} by user {UserId}", 
                order.Id, offerId, userId);
            return RedirectToAction(nameof(MyOrders));
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error creating order from offer {OfferId}", offerId);
            return StatusCode(500, "An error occurred while creating the order.");
        }
    }

    //
    // POST: /Orders/Accept/{id}
    // Seller accepts a buy order — deletes the listing and sets order to Accepted
    //
    [Authorize]
    [HttpPost("Accept/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AcceptOrder(int id)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = User.IsInRole("Admin");
            Log.Information("User {UserId} accepting order {OrderId}", userId, id);

            var order = await _uow.Orders.GetAsync(id);
            if (order == null)
            {
                Log.Warning("Order {OrderId} not found", id);
                return NotFound($"Order with ID {id} not found.");
            }

            var listing = await _uow.Listings.GetAsync(order.ListingId);
            if (listing == null)
            {
                Log.Warning("Listing {ListingId} not found for order {OrderId}", order.ListingId, id);
                return NotFound("Associated listing not found.");
            }

            // Check if user owns the listing or is admin
            if (!isAdmin && listing.SellerId != userId)
            {
                Log.Warning("User {UserId} attempted to accept order {OrderId} for listing they don't own", 
                    userId, id);
                return Forbid("You can only accept orders for your own listings.");
            }

            if (order.Status != "Pending")
            {
                Log.Warning("Order {OrderId} is not in Pending status (current: {Status})", id, order.Status);
                return BadRequest("Only pending orders can be accepted.");
            }

            // Accept the order
            order.Status = "Accepted";
            _uow.Orders.Update(order);

            // Cancel all other pending orders for this listing (since we're accepting this one)
            var allOrders = await _uow.Orders.GetAllAsync();
            var otherPendingOrders = allOrders.Where(o => 
                o.ListingId == listing.Id && 
                o.Id != order.Id && 
                o.Status == "Pending").ToList();
            
            foreach (var otherOrder in otherPendingOrders)
            {
                otherOrder.Status = "Cancelled";
                _uow.Orders.Update(otherOrder);
                Log.Information("Cancelled order {OrderId} because listing {ListingId} was sold", 
                    otherOrder.Id, listing.Id);
            }

            // Reject all pending offers for this listing (since it's been sold)
            var offers = await _uow.Offers.GetByListingAsync(listing.Id);
            foreach (var offer in offers.Where(o => o.Status == "Pending"))
            {
                offer.Status = "Rejected";
                _uow.Offers.Update(offer);
                Log.Information("Rejected offer {OfferId} because listing {ListingId} was sold", 
                    offer.Id, listing.Id);
            }

            // Save all changes
            await _uow.SaveAsync();

            Log.Information("Order {OrderId} accepted successfully by user {UserId}", id, userId);
            
            // Redirect back to seller orders
            if (isAdmin)
            {
                return RedirectToAction(nameof(Index));
            }
            else
            {
                return RedirectToAction(nameof(MySellerOrders));
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error accepting order {OrderId}", id);
            return StatusCode(500, "An error occurred while accepting the order.");
        }
    }

    //
    // POST: /Orders/Complete/{id}
    // Marks an order as completed — allowed for Buyer or Admin (only after seller accepts)
    //
    [Authorize(Roles = "Buyer,Admin")]
    [HttpPost("Complete/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Complete(int id)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = User.IsInRole("Admin");
            Log.Information("User {UserId} completing order {OrderId}", userId, id);

            var order = await _uow.Orders.GetAsync(id);
            if (order == null)
            {
                Log.Warning("Order {OrderId} not found", id);
                return NotFound($"Order with ID {id} not found.");
            }

            // Check if user owns the order or is admin
            if (!isAdmin && order.BuyerId != userId)
            {
                Log.Warning("User {UserId} attempted to complete order {OrderId} they don't own", 
                    userId, id);
                return Forbid("You can only complete your own orders.");
            }

            if (order.Status == "Completed")
            {
                Log.Warning("Order {OrderId} is already completed", id);
                return BadRequest("Order is already completed.");
            }

            // Only allow completing orders that have been accepted by seller
            if (order.Status != "Accepted")
            {
                Log.Warning("Order {OrderId} is not accepted (current: {Status})", id, order.Status);
                return BadRequest("Order must be accepted by seller before it can be completed.");
            }

            order.Status = "Completed";
            _uow.Orders.Update(order);
            
            // Mark the listing as sold when order is completed
            var listing = await _uow.Listings.GetAsync(order.ListingId);
            if (listing != null)
            {
                listing.IsSold = true;
                _uow.Listings.Update(listing);
                Log.Information("Marked listing {ListingId} as sold after order {OrderId} completion", 
                    listing.Id, id);
            }
            
            await _uow.SaveAsync();

            Log.Information("Order {OrderId} completed successfully by user {UserId}", id, userId);
            return RedirectToAction(nameof(MyOrders));
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error completing order {OrderId}", id);
            return StatusCode(500, "An error occurred while completing the order.");
        }
    }
}

