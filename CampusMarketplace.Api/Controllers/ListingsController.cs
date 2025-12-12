using CampusMarketplace.Api.Models;
using CampusMarketplace.Api.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using System.Security.Claims;

namespace CampusMarketplace.Api.Controllers;

// Handles book listing operations (CRUD for books posted by sellers)
[Route("[controller]")]
public class ListingsController : Controller
{
    private readonly IUnitOfWork _uow;

    public ListingsController(IUnitOfWork uow) => _uow = uow;

    //
    // GET: /Listings or /Listings/Index
    // Returns all listings from the database (all books for sale) with sorting, filtering, and paging
    // Anyone can call this - it's public so buyers can browse
    //
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> Index(string sortOrder, string currentFilter, string searchString, int? pageNumber)
    {
        try
        {
            // Log that we're getting listings
            Log.Information("Getting listings with sortOrder: {SortOrder}, searchString: {SearchString}, pageNumber: {PageNumber}", 
                sortOrder, searchString, pageNumber);

            // Set up ViewData for sort parameters (used by view to determine sort direction)
            // Following the Microsoft tutorial pattern exactly
            // Title is the default sort, so it follows the NameSortParm pattern
            ViewData["CurrentSort"] = sortOrder;
            ViewData["TitleSortParm"] = String.IsNullOrEmpty(sortOrder) ? "title_desc" : "";
            ViewData["PriceSortParm"] = sortOrder == "price" ? "price_desc" : "price";
            ViewData["CategorySortParm"] = sortOrder == "category" ? "category_desc" : "category";
            ViewData["ConditionSortParm"] = sortOrder == "condition" ? "condition_desc" : "condition";

            // Handle search string - if new search, reset to page 1
            if (searchString != null)
            {
                pageNumber = 1;
            }
            else
            {
                searchString = currentFilter;
            }

            ViewData["CurrentFilter"] = searchString;

            // Default page size
            int pageSize = 10;

            // Get paged listings from repository
            var listings = await _uow.Listings.GetPagedAsync(
                sortOrder: sortOrder,
                searchString: searchString,
                pageNumber: pageNumber ?? 1,
                pageSize: pageSize);

            // Log how many we found
            Log.Information("Retrieved {Count} listings (Page {PageIndex} of {TotalPages})", 
                listings.Count, listings.PageIndex, listings.TotalPages);

            // Return the view with paginated listings
            return View(listings);
        }
        catch (Exception ex)
        {
            // If something goes wrong, log it and return an error
            Log.Error(ex, "Error retrieving listings");
            return StatusCode(500, "An error occurred while retrieving listings.");
        }
    }

    //
    // GET: /Listings/Details/{id}
    // Returns a single listing by ID (get details about one specific book)
    //
    [HttpGet("Details/{id}")]
    [AllowAnonymous]
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        try
        {
            // Log which listing we're looking for
            Log.Information("Getting listing with ID: {ListingId}", id);
            
            // Get the listing from the database
            var listing = await _uow.Listings.GetAsync(id.Value);
            
            // If it doesn't exist, return 404 Not Found
            if (listing == null)
            {
                Log.Warning("Listing with ID {ListingId} not found", id);
                return NotFound();
            }

            // Log success and return the view
            Log.Information("Successfully retrieved listing {ListingId}", id);
            return View(listing);
        }
        catch (Exception ex)
        {
            // If something goes wrong, log it and return an error
            Log.Error(ex, "Error retrieving listing {ListingId}", id);
            return StatusCode(500, "An error occurred while retrieving the listing.");
        }
    }

    //
    // GET: /Listings/Create
    // Shows the create form
    //
    [Authorize(Roles = "Seller,Admin")]
    [HttpGet("Create")]
    public async Task<IActionResult> Create()
    {
        var categories = await _uow.Categories.GetAllAsync();
        ViewData["CategoryId"] = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(categories, "Id", "Name");
        return View();
    }

    //
    // POST: /Listings/Create
    // Creates a new listing (seller posts a book for sale)
    // Only Sellers and Admins can create listings - regular buyers can't post books
    //
    [Authorize(Roles = "Seller,Admin")]
    [HttpPost("Create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Listing listing)
    {
        try
        {
            // Get the ID of the user making the request
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            Log.Information("User {UserId} creating new listing: {Title}", userId, listing.Title);

            // Remove fields from validation that are set by the controller, not the form
            ModelState.Remove("SellerId");
            ModelState.Remove("Seller");
            ModelState.Remove("Category");
            ModelState.Remove("Offers");
            ModelState.Remove("Orders");
            
            // Check if the data is valid (like checking if title is provided)
            if (!ModelState.IsValid)
            {
                Log.Warning("Invalid model state for listing creation by user {UserId}", userId);
                var categories = await _uow.Categories.GetAllAsync();
                ViewData["CategoryId"] = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(categories, "Id", "Name", listing.CategoryId);
                return View(listing);
            }

            // Set the seller ID to the current user's ID
            // This makes sure the listing is linked to the person who created it
            listing.SellerId = userId ?? throw new UnauthorizedAccessException("User ID not found");
            
            // Add the new listing to the database
            await _uow.Listings.AddAsync(listing);
            // Save the changes (actually write to database)
            await _uow.SaveAsync();

            // Log success
            Log.Information("Listing {ListingId} created successfully by user {UserId}", listing.Id, userId);
            
            // Redirect to index
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            // If something goes wrong, log it and return an error
            Log.Error(ex, "Error creating listing by user {UserId}", User.FindFirstValue(ClaimTypes.NameIdentifier));
            var categories = await _uow.Categories.GetAllAsync();
            ViewData["CategoryId"] = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(categories, "Id", "Name", listing.CategoryId);
            return View(listing);
        }
    }

    //
    // GET: /Listings/Edit/{id}
    // Shows the edit form
    //
    [Authorize(Roles = "Seller,Admin")]
    [HttpGet("Edit/{id}")]
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var listing = await _uow.Listings.GetAsync(id.Value);
        if (listing == null)
        {
            return NotFound();
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var isAdmin = User.IsInRole("Admin");
        if (!isAdmin && listing.SellerId != userId)
        {
            return Forbid();
        }

        var categories = await _uow.Categories.GetAllAsync();
        ViewData["CategoryId"] = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(categories, "Id", "Name", listing.CategoryId);
        return View(listing);
    }

    //
    // POST: /Listings/Edit/{id}
    // Updates an existing listing (seller changes price, description, etc.)
    // Only the seller who owns the listing OR an Admin can update it
    //
    [Authorize(Roles = "Seller,Admin")]
    [HttpPost("Edit/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Listing listing)
    {
        if (id != listing.Id)
        {
            return NotFound();
        }

        try
        {
            // Get the current user's ID and check if they're an admin
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = User.IsInRole("Admin");
            
            Log.Information("User {UserId} updating listing {ListingId}", userId, id);

            // First, get the existing listing from the database
            var existingListing = await _uow.Listings.GetAsync(id);
            
            // If it doesn't exist, return 404
            if (existingListing == null)
            {
                Log.Warning("Listing {ListingId} not found for update", id);
                return NotFound();
            }

            // Check if user owns the listing OR is an admin
            // You can only update your own listings (unless you're an admin)
            if (!isAdmin && existingListing.SellerId != userId)
            {
                Log.Warning("User {UserId} attempted to update listing {ListingId} owned by {SellerId}", 
                    userId, id, existingListing.SellerId);
                return Forbid();
            }

            if (!ModelState.IsValid)
            {
                var categories = await _uow.Categories.GetAllAsync();
                ViewData["CategoryId"] = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(categories, "Id", "Name", listing.CategoryId);
                return View(listing);
            }

            // Update the listing fields with new values
            existingListing.Title = listing.Title;
            existingListing.Description = listing.Description;
            existingListing.Price = listing.Price;
            existingListing.Condition = listing.Condition;
            existingListing.CategoryId = listing.CategoryId;

            // Mark the listing as updated
            _uow.Listings.Update(existingListing);
            // Save changes to database
            await _uow.SaveAsync();

            // Log success
            Log.Information("Listing {ListingId} updated successfully by user {UserId}", id, userId);
            
            // Redirect to index
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            // If something goes wrong, log it and return an error
            Log.Error(ex, "Error updating listing {ListingId}", id);
            var categories = await _uow.Categories.GetAllAsync();
            ViewData["CategoryId"] = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(categories, "Id", "Name", listing.CategoryId);
            return View(listing);
        }
    }

    //
    // GET: /Listings/Delete/{id}
    // Shows the delete confirmation
    //
    [Authorize(Roles = "Seller,Admin")]
    [HttpGet("Delete/{id}")]
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var listing = await _uow.Listings.GetAsync(id.Value);
        if (listing == null)
        {
            return NotFound();
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var isAdmin = User.IsInRole("Admin");
        if (!isAdmin && listing.SellerId != userId)
        {
            return Forbid();
        }

        return View(listing);
    }

    //
    // POST: /Listings/Delete/{id}
    // Deletes a listing (seller removes their book from sale)
    // Only the seller who owns the listing OR an Admin can delete it
    //
    [Authorize(Roles = "Seller,Admin")]
    [HttpPost("Delete/{id}")]
    [ValidateAntiForgeryToken]
    [ActionName("Delete")]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        try
        {
            // Get the current user's ID and check if they're an admin
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = User.IsInRole("Admin");
            
            Log.Information("User {UserId} deleting listing {ListingId}", userId, id);

            // Get the listing we want to delete
            var listing = await _uow.Listings.GetAsync(id);
            
            // If it doesn't exist, return 404
            if (listing == null)
            {
                Log.Warning("Listing {ListingId} not found for deletion", id);
                return NotFound();
            }

            // Check if user owns the listing OR is an admin
            // You can only delete your own listings (unless you're an admin)
            if (!isAdmin && listing.SellerId != userId)
            {
                Log.Warning("User {UserId} attempted to delete listing {ListingId} owned by {SellerId}", 
                    userId, id, listing.SellerId);
                return Forbid();
            }

            // Get all related offers and orders for this listing
            var offers = await _uow.Offers.GetByListingAsync(id);
            var orders = await _uow.Orders.GetAllAsync(o => o.ListingId == id);

            // Delete all related offers first (to avoid foreign key constraint errors)
            foreach (var offer in offers)
            {
                _uow.Offers.Remove(offer);
                Log.Information("Removing offer {OfferId} for listing {ListingId}", offer.Id, id);
            }

            // Delete all related orders first (reviews will be cascade deleted)
            foreach (var order in orders)
            {
                _uow.Orders.Remove(order);
                Log.Information("Removing order {OrderId} for listing {ListingId}", order.Id, id);
            }

            // Mark the listing for deletion
            _uow.Listings.Remove(listing);
            
            // Save all changes (offers, orders, and listing) in one transaction
            await _uow.SaveAsync();

            // Log success
            Log.Information("Listing {ListingId} deleted successfully by user {UserId} (removed {OfferCount} offers and {OrderCount} orders)", 
                id, userId, offers.Count(), orders.Count());
            
            // Redirect to index
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            // If something goes wrong, log it and return an error
            Log.Error(ex, "Error deleting listing {ListingId}", id);
            return StatusCode(500, "An error occurred while deleting the listing.");
        }
    }
}
