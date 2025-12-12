using CampusMarketplace.Api.Models;
using CampusMarketplace.Api.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Serilog;

namespace CampusMarketplace.Api.Controllers;

// Handles category management (Admin only) - categories organize book listings
[Route("[controller]")]
[Authorize(Roles = "Admin")]
public class CategoriesController : Controller
{
    private readonly IUnitOfWork _uow;

    public CategoriesController(IUnitOfWork uow) => _uow = uow;

    //
    // GET: /Categories or /Categories/Index
    // Returns all categories from the database
    //
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        try
        {
            // Log that we're about to get all categories (helps us debug later)
            Log.Information("Getting all categories");
            
            // Ask the repository to get all categories from the database
            var categories = await _uow.Categories.GetAllAsync();
            
            // Log how many we found (useful for debugging)
            Log.Information("Retrieved {Count} categories", categories.Count());
            
            // Return the view with categories
            return View(categories);
        }
        catch (Exception ex)
        {
            // If something goes wrong, log the error and return a 500 error
            // This catches any unexpected problems (like database connection issues)
            Log.Error(ex, "Error retrieving categories");
            return StatusCode(500, "An error occurred while retrieving categories.");
        }
    }

    //
    // GET: /Categories/Details/{id}
    // Returns a single category by ID along with all its listings
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
            // Log which category we're looking for
            Log.Information("Getting category with ID: {CategoryId}", id);
            
            // Get the category and include all its related listings
            var category = await _uow.Categories.GetCategoryWithListingsAsync(id.Value);
            
            // If we didn't find a category with that ID, return 404 Not Found
            if (category == null)
            {
                Log.Warning("Category with ID {CategoryId} not found", id);
                return NotFound();
            }

            // Log success and how many listings this category has
            Log.Information("Successfully retrieved category {CategoryId} with {Count} listings", 
                id, category.Listings.Count);
            
            // Return the view
            return View(category);
        }
        catch (Exception ex)
        {
            // If something goes wrong, log it and return an error
            Log.Error(ex, "Error retrieving category {CategoryId}", id);
            return StatusCode(500, "An error occurred while retrieving the category.");
        }
    }

    //
    // GET: /Categories/Create
    // Shows the create form
    //
    [HttpGet("Create")]
    public IActionResult Create()
    {
        return View();
    }

    //
    // POST: /Categories/Create
    // Creates a new category (like adding a new folder for organizing books)
    // Only Admins can do this - regular users can't create categories
    //
    [HttpPost("Create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Category category)
    {
        try
        {
            // Log that an admin is creating a new category
            Log.Information("Admin creating new category: {CategoryName}", category.Name);

            // Check if the data sent is valid (like checking if name is provided)
            if (!ModelState.IsValid)
            {
                Log.Warning("Invalid model state for category creation");
                return View(category);
            }

            // Add the new category to the database
            await _uow.Categories.AddAsync(category);
            // Save the changes (actually write to database)
            await _uow.SaveAsync();

            // Log that it was created successfully
            Log.Information("Category {CategoryId} created successfully", category.Id);
            
            // Redirect to index
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            // If something goes wrong, log it and return an error
            Log.Error(ex, "Error creating category");
            return View(category);
        }
    }

    //
    // GET: /Categories/Edit/{id}
    // Shows the edit form
    //
    [HttpGet("Edit/{id}")]
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var category = await _uow.Categories.GetAsync(id.Value);
        if (category == null)
        {
            return NotFound();
        }
        return View(category);
    }

    //
    // POST: /Categories/Edit/{id}
    // Updates an existing category (changes its name or description)
    // Only Admins can do this
    //
    [HttpPost("Edit/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Category category)
    {
        if (id != category.Id)
        {
            return NotFound();
        }

        try
        {
            // Log that an admin is updating a category
            Log.Information("Admin updating category {CategoryId}", id);

            // First, get the existing category from the database
            var existingCategory = await _uow.Categories.GetAsync(id);
            
            // If it doesn't exist, return 404 Not Found
            if (existingCategory == null)
            {
                Log.Warning("Category {CategoryId} not found for update", id);
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                return View(category);
            }

            // Update the fields with new values
            existingCategory.Name = category.Name;
            existingCategory.Description = category.Description;

            // Tell the database this category has been updated
            _uow.Categories.Update(existingCategory);
            // Save the changes to the database
            await _uow.SaveAsync();

            // Log success
            Log.Information("Category {CategoryId} updated successfully", id);
            
            // Redirect to index
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            // If something goes wrong, log it and return an error
            Log.Error(ex, "Error updating category {CategoryId}", id);
            return View(category);
        }
    }

    //
    // GET: /Categories/Delete/{id}
    // Shows the delete confirmation
    //
    [HttpGet("Delete/{id}")]
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var category = await _uow.Categories.GetAsync(id.Value);
        if (category == null)
        {
            return NotFound();
        }

        return View(category);
    }

    //
    // POST: /Categories/Delete/{id}
    // Deletes a category from the database
    // Only Admins can do this - be careful, this will delete all listings in that category!
    //
    [HttpPost("Delete/{id}")]
    [ValidateAntiForgeryToken]
    [ActionName("Delete")]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        try
        {
            // Log that an admin is deleting a category
            Log.Information("Admin deleting category {CategoryId}", id);

            // First, get the category we want to delete
            var category = await _uow.Categories.GetAsync(id);
            
            // If it doesn't exist, return 404 Not Found
            if (category == null)
            {
                Log.Warning("Category {CategoryId} not found for deletion", id);
                return NotFound();
            }

            // Mark this category for deletion
            _uow.Categories.Remove(category);
            // Actually delete it from the database
            await _uow.SaveAsync();

            // Log success
            Log.Information("Category {CategoryId} deleted successfully", id);
            
            // Redirect to index
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            // If something goes wrong, log it and return an error
            Log.Error(ex, "Error deleting category {CategoryId}", id);
            return StatusCode(500, "An error occurred while deleting the category.");
        }
    }
}
