using CampusMarketplace.Api.Models;
using CampusMarketplace.Api.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using System.Security.Claims;

namespace CampusMarketplace.Api.Controllers;

// Handles offer operations (buyers make price offers on listings, sellers accept/reject them)
[Route("[controller]")]
[Authorize]
public class OffersController : Controller
{
    private readonly IUnitOfWork _uow;

    public OffersController(IUnitOfWork uow) => _uow = uow;

    //
    // GET: /Offers or /Offers/Index
    // Returns all offers — Admin only
    //
    [Authorize(Roles = "Admin")]
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        try
        {
            Log.Information("Admin retrieving all offers");
            var offers = await _uow.Offers.GetAllAsync();
            // Load related entities
            foreach (var offer in offers)
            {
                if (offer.ListingId > 0)
                {
                    var listing = await _uow.Listings.GetAsync(offer.ListingId);
                    if (listing != null)
                    {
                        offer.Listing = listing;
                    }
                }
            }
            Log.Information("Retrieved {Count} offers", offers.Count());
            return View(offers);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error retrieving offers");
            return StatusCode(500, "An error occurred while retrieving offers.");
        }
    }

    //
    // GET: /Offers/ByListing/{listingId}
    // Returns all offers for a specific listing (for sellers)
    //
    [HttpGet("ByListing/{listingId}")]
    public async Task<IActionResult> ByListing(int listingId)
    {
        try
        {
            Log.Information("Getting offers for listing {ListingId}", listingId);
            
            var listing = await _uow.Listings.GetAsync(listingId);
            if (listing == null)
            {
                Log.Warning("Listing {ListingId} not found", listingId);
                return NotFound();
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = User.IsInRole("Admin");
            var isSeller = User.IsInRole("Seller") && listing.SellerId == userId;

            // Only allow seller or admin to view offers on their listings
            if (!isAdmin && !isSeller)
            {
                Log.Warning("User {UserId} attempted to view offers for listing {ListingId} they don't own", 
                    userId, listingId);
                return Forbid();
            }

            var offers = await _uow.Offers.GetByListingAsync(listingId);
            Log.Information("Retrieved {Count} offers for listing {ListingId}", offers.Count(), listingId);
            ViewData["Listing"] = listing;
            return View(offers);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error retrieving offers for listing {ListingId}", listingId);
            return StatusCode(500, "An error occurred while retrieving offers.");
        }
    }

    //
    // GET: /Offers/MyOffers
    // Returns all offers made by the current buyer
    //
    [Authorize(Roles = "Buyer")]
    [HttpGet("MyOffers")]
    public async Task<IActionResult> MyOffers()
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            Log.Information("User {UserId} retrieving their offers", userId);
            
            var offers = await _uow.Offers.GetByBuyerAsync(userId!);
            Log.Information("User {UserId} has {Count} offers", userId, offers.Count());
            return View(offers);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error retrieving user offers");
            return StatusCode(500, "An error occurred while retrieving your offers.");
        }
    }

    //
    // GET: /Offers/Details/{id}
    // Returns a single offer by ID
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
            Log.Information("Getting offer with ID: {OfferId}", id);
            var offer = await _uow.Offers.GetAsync(id.Value);
            
            if (offer == null)
            {
                Log.Warning("Offer with ID {OfferId} not found", id);
                return NotFound();
            }

            // Load related entities
            var listing = await _uow.Listings.GetAsync(offer.ListingId);
            if (listing != null)
            {
                offer.Listing = listing;
                var category = await _uow.Categories.GetAsync(listing.CategoryId);
                if (category != null)
                {
                    listing.Category = category;
                }
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = User.IsInRole("Admin");
            var isBuyer = offer.BuyerId == userId;
            var isSeller = offer.Listing != null && offer.Listing.SellerId == userId;

            // Only allow buyer, seller, or admin to view the offer
            if (!isAdmin && !isBuyer && !isSeller)
            {
                Log.Warning("User {UserId} attempted to view offer {OfferId} they don't have access to", 
                    userId, id);
                return Forbid();
            }

            Log.Information("Successfully retrieved offer {OfferId}", id);
            return View(offer);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error retrieving offer {OfferId}", id);
            return StatusCode(500, "An error occurred while retrieving the offer.");
        }
    }

    //
    // GET: /Offers/Create
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
            return BadRequest("You cannot make an offer on your own listing.");
        }

        // Prevent creating offers for sold listings
        if (listing.IsSold)
        {
            return BadRequest("This listing has already been sold and is no longer available.");
        }

        ViewData["ListingId"] = listingId;
        ViewData["Listing"] = listing;
        return View(new Offer { ListingId = listingId.Value });
    }

    //
    // POST: /Offers/Create
    // Creates a new offer — only allowed for Buyers
    //
    [Authorize(Roles = "Buyer")]
    [HttpPost("Create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Offer offer)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            Log.Information("User {UserId} creating offer for listing {ListingId}", userId, offer.ListingId);

            // Remove fields from validation that are set by the controller, not the form
            ModelState.Remove("BuyerId");
            ModelState.Remove("Buyer");
            ModelState.Remove("Listing");
            ModelState.Remove("Status");
            ModelState.Remove("CreatedAt");

            if (!ModelState.IsValid)
            {
                Log.Warning("Invalid model state for offer creation by user {UserId}", userId);
                var listingForView = await _uow.Listings.GetAsync(offer.ListingId);
                ViewData["ListingId"] = offer.ListingId;
                ViewData["Listing"] = listingForView;
                return View(offer);
            }

            // Verify listing exists
            var listing = await _uow.Listings.GetAsync(offer.ListingId);
            if (listing == null)
            {
                Log.Warning("Listing {ListingId} not found for offer creation", offer.ListingId);
                return NotFound();
            }

            // Prevent buying your own listing
            if (listing.SellerId == userId)
            {
                Log.Warning("User {UserId} attempted to make offer on their own listing {ListingId}", 
                    userId, offer.ListingId);
                return BadRequest("You cannot make an offer on your own listing.");
            }

            // Prevent creating offers for sold listings
            if (listing.IsSold)
            {
                Log.Warning("User {UserId} attempted to make offer on sold listing {ListingId}", 
                    userId, offer.ListingId);
                return BadRequest("This listing has already been sold and is no longer available.");
            }

            offer.BuyerId = userId!;
            offer.Status = "Pending";
            offer.CreatedAt = DateTime.UtcNow;

            await _uow.Offers.AddAsync(offer);
            await _uow.SaveAsync();

            Log.Information("Offer {OfferId} created successfully by user {UserId}", offer.Id, userId);
            return RedirectToAction(nameof(MyOffers));
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error creating offer");
            var listing = await _uow.Listings.GetAsync(offer.ListingId);
            ViewData["ListingId"] = offer.ListingId;
            ViewData["Listing"] = listing;
            return View(offer);
        }
    }

    //
    // POST: /Offers/Accept/{id}
    // Accepts an offer — only allowed for Seller who owns the listing or Admin
    //
    [HttpPost("Accept/{id}")]
    [ValidateAntiForgeryToken]
    [Authorize]
    public async Task<IActionResult> Accept(int id)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = User.IsInRole("Admin");
            var isSeller = User.IsInRole("Seller");
            
            // Check if user has required role
            if (!isAdmin && !isSeller)
            {
                Log.Warning("User {UserId} attempted to accept offer {OfferId} without required role", userId, id);
                return Forbid("You must be a Seller or Admin to accept offers.");
            }
            
            Log.Information("User {UserId} accepting offer {OfferId}", userId, id);

            var offer = await _uow.Offers.GetAsync(id);
            if (offer == null)
            {
                Log.Warning("Offer {OfferId} not found", id);
                return NotFound($"Offer with ID {id} not found.");
            }

            var listing = await _uow.Listings.GetAsync(offer.ListingId);
            if (listing == null)
            {
                Log.Error("Listing {ListingId} not found for offer {OfferId}", offer.ListingId, id);
                return StatusCode(500, "Associated listing not found.");
            }

            // Check if user owns the listing or is admin
            if (!isAdmin && listing.SellerId != userId)
            {
                Log.Warning("User {UserId} attempted to accept offer {OfferId} on listing they don't own", 
                    userId, id);
                return Forbid("You can only accept offers on your own listings.");
            }

            if (offer.Status != "Pending")
            {
                Log.Warning("Offer {OfferId} is not in Pending status (current: {Status})", id, offer.Status);
                return BadRequest("Only pending offers can be accepted.");
            }

            offer.Status = "Accepted";
            _uow.Offers.Update(offer);
            await _uow.SaveAsync();

            Log.Information("Offer {OfferId} accepted successfully by user {UserId}", id, userId);
            // Redirect back to the listing's offers page for sellers, or Index for admins
            if (isAdmin)
            {
                return RedirectToAction(nameof(Index));
            }
            else
            {
                return RedirectToAction(nameof(ByListing), new { listingId = listing.Id });
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error accepting offer {OfferId}", id);
            return StatusCode(500, "An error occurred while accepting the offer.");
        }
    }

    //
    // POST: /Offers/Reject/{id}
    // Rejects an offer — only allowed for Seller who owns the listing or Admin
    //
    [HttpPost("Reject/{id}")]
    [ValidateAntiForgeryToken]
    [Authorize]
    public async Task<IActionResult> Reject(int id)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = User.IsInRole("Admin");
            var isSeller = User.IsInRole("Seller");
            
            // Check if user has required role
            if (!isAdmin && !isSeller)
            {
                Log.Warning("User {UserId} attempted to reject offer {OfferId} without required role", userId, id);
                return Forbid("You must be a Seller or Admin to reject offers.");
            }
            
            Log.Information("User {UserId} rejecting offer {OfferId}", userId, id);

            var offer = await _uow.Offers.GetAsync(id);
            if (offer == null)
            {
                Log.Warning("Offer {OfferId} not found", id);
                return NotFound($"Offer with ID {id} not found.");
            }

            var listing = await _uow.Listings.GetAsync(offer.ListingId);
            if (listing == null)
            {
                Log.Error("Listing {ListingId} not found for offer {OfferId}", offer.ListingId, id);
                return StatusCode(500, "Associated listing not found.");
            }

            // Check if user owns the listing or is admin
            if (!isAdmin && listing.SellerId != userId)
            {
                Log.Warning("User {UserId} attempted to reject offer {OfferId} on listing they don't own", 
                    userId, id);
                return Forbid("You can only reject offers on your own listings.");
            }

            if (offer.Status != "Pending")
            {
                Log.Warning("Offer {OfferId} is not in Pending status (current: {Status})", id, offer.Status);
                return BadRequest("Only pending offers can be rejected.");
            }

            offer.Status = "Rejected";
            _uow.Offers.Update(offer);
            await _uow.SaveAsync();

            Log.Information("Offer {OfferId} rejected successfully by user {UserId}", id, userId);
            // Redirect back to the listing's offers page for sellers, or Index for admins
            if (isAdmin)
            {
                return RedirectToAction(nameof(Index));
            }
            else
            {
                return RedirectToAction(nameof(ByListing), new { listingId = listing.Id });
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error rejecting offer {OfferId}", id);
            return StatusCode(500, "An error occurred while rejecting the offer.");
        }
    }
}

