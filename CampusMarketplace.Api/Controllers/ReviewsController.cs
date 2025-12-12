using CampusMarketplace.Api.Models;
using CampusMarketplace.Api.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using System.Security.Claims;

namespace CampusMarketplace.Api.Controllers;

// Handles review operations (buyers leave reviews after completing orders)
[Route("[controller]")]
[Authorize]
public class ReviewsController : Controller
{
    private readonly IUnitOfWork _uow;

    public ReviewsController(IUnitOfWork uow) => _uow = uow;

    //
    // GET: /Reviews or /Reviews/Index
    // Returns all reviews — Admin only
    //
    [Authorize(Roles = "Admin")]
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        try
        {
            Log.Information("Admin retrieving all reviews");
            var reviews = await _uow.Reviews.GetAllAsync();
            Log.Information("Retrieved {Count} reviews", reviews.Count());
            return View(reviews);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error retrieving reviews");
            return StatusCode(500, "An error occurred while retrieving reviews.");
        }
    }

    //
    // GET: /Reviews/MyReviews
    // Returns all reviews written by the current user
    //
    [HttpGet("MyReviews")]
    public async Task<IActionResult> MyReviews()
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            Log.Information("User {UserId} retrieving their written reviews", userId);
            
            var reviews = await _uow.Reviews.GetByReviewerAsync(userId!);
            Log.Information("User {UserId} has written {Count} reviews", userId, reviews.Count());
            return View(reviews);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error retrieving user reviews");
            return StatusCode(500, "An error occurred while retrieving your reviews.");
        }
    }

    //
    // GET: /Reviews/Details/{id}
    // Returns a single review by ID
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
            Log.Information("Getting review with ID: {ReviewId}", id);
            var review = await _uow.Reviews.GetAsync(id.Value);
            
            if (review == null)
            {
                Log.Warning("Review with ID {ReviewId} not found", id);
                return NotFound();
            }

            Log.Information("Successfully retrieved review {ReviewId}", id);
            return View(review);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error retrieving review {ReviewId}", id);
            return StatusCode(500, "An error occurred while retrieving the review.");
        }
    }

    //
    // GET: /Reviews/Create
    // Shows the create form
    //
    [Authorize(Roles = "Buyer")]
    [HttpGet("Create")]
    public async Task<IActionResult> Create(int? orderId)
    {
        if (orderId == null)
        {
            return NotFound();
        }

        var order = await _uow.Orders.GetWithReviewAsync(orderId.Value);
        if (order == null)
        {
            return NotFound();
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (order.BuyerId != userId)
        {
            return Forbid();
        }

        if (order.Status != "Accepted" && order.Status != "Completed")
        {
            return BadRequest("You can only review orders that have been accepted by the seller.");
        }

        if (order.Review != null)
        {
            return BadRequest("A review already exists for this order.");
        }

        ViewData["Order"] = order;
        return View(new Review { OrderId = orderId.Value });
    }

    //
    // POST: /Reviews/Create
    // Creates a new review — only allowed for Buyers who completed the order
    //
    [Authorize(Roles = "Buyer")]
    [HttpPost("Create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Review review)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            Log.Information("User {UserId} creating review for order {OrderId}", userId, review.OrderId);

            // Remove fields from validation that are set by the controller, not the form
            ModelState.Remove("ReviewerId");
            ModelState.Remove("RevieweeId");
            ModelState.Remove("Reviewer");
            ModelState.Remove("Reviewee");
            ModelState.Remove("Order");
            ModelState.Remove("CreatedAt");

            if (!ModelState.IsValid)
            {
                Log.Warning("Invalid model state for review creation by user {UserId}", userId);
                var orderForView = await _uow.Orders.GetWithReviewAsync(review.OrderId);
                ViewData["Order"] = orderForView;
                return View(review);
            }

            // Validate rating range
            if (review.Rating < 1 || review.Rating > 5)
            {
                Log.Warning("Invalid rating {Rating} provided by user {UserId}", review.Rating, userId);
                return BadRequest("Rating must be between 1 and 5.");
            }

            // Verify order exists and is completed
            var order = await _uow.Orders.GetWithReviewAsync(review.OrderId);
            if (order == null)
            {
                Log.Warning("Order {OrderId} not found for review creation", review.OrderId);
                return NotFound($"Order with ID {review.OrderId} not found.");
            }

            // Check if order belongs to user
            if (order.BuyerId != userId)
            {
                Log.Warning("User {UserId} attempted to create review for order {OrderId} they don't own", 
                    userId, review.OrderId);
                return Forbid("You can only review orders you placed.");
            }

            // Check if order is accepted or completed (seller has accepted the purchase)
            if (order.Status != "Accepted" && order.Status != "Completed")
            {
                Log.Warning("Order {OrderId} is not accepted or completed (status: {Status})", review.OrderId, order.Status);
                return BadRequest("You can only review orders that have been accepted by the seller.");
            }

            // Check if review already exists for this order
            if (order.Review != null)
            {
                Log.Warning("Review already exists for order {OrderId}", review.OrderId);
                return BadRequest("A review already exists for this order.");
            }

            // Get listing to determine reviewee (seller)
            var listing = await _uow.Listings.GetAsync(order.ListingId);
            if (listing == null)
            {
                Log.Error("Listing {ListingId} not found for order {OrderId}", order.ListingId, review.OrderId);
                return StatusCode(500, "Associated listing not found.");
            }

            // Prevent self-review
            if (listing.SellerId == userId)
            {
                Log.Warning("User {UserId} attempted to review themselves", userId);
                return BadRequest("You cannot review yourself.");
            }

            review.ReviewerId = userId!;
            review.RevieweeId = listing.SellerId;
            review.CreatedAt = DateTime.UtcNow;

            await _uow.Reviews.AddAsync(review);
            await _uow.SaveAsync();

            Log.Information("Review {ReviewId} created successfully by user {UserId} for order {OrderId}", 
                review.Id, userId, review.OrderId);
            return RedirectToAction(nameof(MyReviews));
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error creating review");
            var order = await _uow.Orders.GetWithReviewAsync(review.OrderId);
            ViewData["Order"] = order;
            return View(review);
        }
    }

    //
    // GET: /Reviews/Edit/{id}
    // Shows the edit form
    //
    [Authorize(Roles = "Buyer")]
    [HttpGet("Edit/{id}")]
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var review = await _uow.Reviews.GetAsync(id.Value);
        if (review == null)
        {
            return NotFound();
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (review.ReviewerId != userId)
        {
            return Forbid();
        }

        return View(review);
    }

    //
    // POST: /Reviews/Edit/{id}
    // Updates an existing review — only allowed for reviewer
    //
    [Authorize(Roles = "Buyer")]
    [HttpPost("Edit/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Review review)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            Log.Information("User {UserId} updating review {ReviewId}", userId, id);

            var existingReview = await _uow.Reviews.GetAsync(id);
            if (existingReview == null)
            {
                Log.Warning("Review {ReviewId} not found for update", id);
                return NotFound($"Review with ID {id} not found.");
            }

            // Check if user owns the review
            if (existingReview.ReviewerId != userId)
            {
                Log.Warning("User {UserId} attempted to update review {ReviewId} they don't own", 
                    userId, id);
                return Forbid("You can only update your own reviews.");
            }

            // Validate rating range
            if (review.Rating < 1 || review.Rating > 5)
            {
                Log.Warning("Invalid rating {Rating} provided by user {UserId}", review.Rating, userId);
                return BadRequest("Rating must be between 1 and 5.");
            }

            existingReview.Rating = review.Rating;
            existingReview.Comment = review.Comment;

            _uow.Reviews.Update(existingReview);
            await _uow.SaveAsync();

            Log.Information("Review {ReviewId} updated successfully by user {UserId}", id, userId);
            return RedirectToAction(nameof(MyReviews));
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error updating review {ReviewId}", id);
            return View(review);
        }
    }

    //
    // GET: /Reviews/Delete/{id}
    // Shows the delete confirmation
    //
    [Authorize(Roles = "Buyer,Admin")]
    [HttpGet("Delete/{id}")]
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var review = await _uow.Reviews.GetAsync(id.Value);
        if (review == null)
        {
            return NotFound();
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var isAdmin = User.IsInRole("Admin");
        if (!isAdmin && review.ReviewerId != userId)
        {
            return Forbid();
        }

        return View(review);
    }

    //
    // POST: /Reviews/Delete/{id}
    // Deletes a review — only allowed for reviewer or Admin
    //
    [Authorize(Roles = "Buyer,Admin")]
    [HttpPost("Delete/{id}")]
    [ValidateAntiForgeryToken]
    [ActionName("Delete")]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = User.IsInRole("Admin");
            Log.Information("User {UserId} deleting review {ReviewId}", userId, id);

            var review = await _uow.Reviews.GetAsync(id);
            if (review == null)
            {
                Log.Warning("Review {ReviewId} not found for deletion", id);
                return NotFound($"Review with ID {id} not found.");
            }

            // Check if user owns the review or is admin
            if (!isAdmin && review.ReviewerId != userId)
            {
                Log.Warning("User {UserId} attempted to delete review {ReviewId} they don't own", 
                    userId, id);
                return Forbid("You can only delete your own reviews.");
            }

            _uow.Reviews.Remove(review);
            await _uow.SaveAsync();

            Log.Information("Review {ReviewId} deleted successfully by user {UserId}", id, userId);
            return RedirectToAction(nameof(MyReviews));
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error deleting review {ReviewId}", id);
            return StatusCode(500, "An error occurred while deleting the review.");
        }
    }
}

