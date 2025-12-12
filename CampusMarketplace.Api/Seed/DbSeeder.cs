using CampusMarketplace.Api.Data;
using CampusMarketplace.Api.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using Serilog;

namespace CampusMarketplace.Api.Seed;

// Seeds initial data into the database (roles, users, categories, and book listings)
public static class DbSeeder
{
    public static async Task SeedAsync(IServiceProvider sp)
    {
        using var scope = sp.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        // Apply pending database migrations before seeding
        try
        {
            Log.Information("Checking for pending database migrations...");
            
            if (!await ctx.Database.CanConnectAsync())
            {
                Log.Warning("Cannot connect to database. Will attempt to create it during migration.");
            }
            
            var pendingMigrations = (await ctx.Database.GetPendingMigrationsAsync()).ToList();
            if (pendingMigrations.Any())
            {
                Log.Information("Applying {Count} pending migrations: {Migrations}", 
                    pendingMigrations.Count(), 
                    string.Join(", ", pendingMigrations));
                await ctx.Database.MigrateAsync();
                Log.Information("All migrations applied successfully.");
            }
            else
            {
                Log.Information("Database is up to date. No migrations needed.");
            }
        }
        catch (Microsoft.Data.SqlClient.SqlException ex)
        {
            Log.Error(ex, "SQL error during migration. Error number: {ErrorNumber}", ex.Number);
            if (ex.Number == 2714)
            {
                Log.Warning("Database tables already exist. Continuing with seeding.");
            }
            else
            {
                throw;
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Critical error during database migration. Application cannot continue.");
            throw;
        }

        // Create roles if they don't exist
        string[] roles = { "Admin", "Seller", "Buyer" };
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));
        }

        // Helper to create users only if they don't exist (preserves existing users)
        async Task<AppUser> EnsureUser(string email, string role)
        {
            var user = await userManager.FindByEmailAsync(email);
            if (user == null)
            {
                user = new AppUser { UserName = email, Email = email, EmailConfirmed = true };
                await userManager.CreateAsync(user, "Passw0rd!");
                await userManager.AddToRoleAsync(user, role);
                Log.Information("Created default user: {Email} with role: {Role}", email, role);
            }
            else
            {
                Log.Information("Default user already exists: {Email}. Preserving existing user data.", email);
            }
            return user;
        }

        // Create default test users (only if they don't exist)
        var admin = await EnsureUser("admin@campus.local", "Admin");
        var seller1 = await EnsureUser("alice@campus.local", "Seller");
        var seller2 = await EnsureUser("charlie@campus.local", "Seller");
        var seller3 = await EnsureUser("diana@campus.local", "Seller");
        var buyer = await EnsureUser("bob@campus.local", "Buyer");
        
        Log.Information("Default users ensured. All other users in the database are preserved.");

        // Create categories if they don't exist
        var categories = new List<Category>
        {
            new Category { Name = "Computer Science", Description = "Programming and tech books" },
            new Category { Name = "Mathematics", Description = "Algebra, calculus, and statistics" },
            new Category { Name = "Literature", Description = "Novels and plays" },
            new Category { Name = "Biology", Description = "Life sciences and biology textbooks" },
            new Category { Name = "Physics", Description = "Physics and engineering textbooks" },
            new Category { Name = "Business", Description = "Business and economics books" },
            new Category { Name = "History", Description = "History and social studies" }
        };

        foreach (var category in categories)
        {
            if (!await ctx.Categories.AnyAsync(c => c.Name == category.Name))
            {
                ctx.Categories.Add(category);
            }
        }
        await ctx.SaveChangesAsync();

        // Only seed listings if database is empty (preserves existing data)
        var existingListingsCount = await ctx.Listings.CountAsync();
        if (existingListingsCount > 0)
        {
            Log.Information("Database already contains {Count} listings. Skipping seeding to preserve ALL existing data.", existingListingsCount);
            Log.Information("All users, listings, orders, offers, and reviews are preserved and will persist across restarts.");
            Log.Information("Database seeding completed successfully.");
            return;
        }
        
        Log.Information("Database is empty. Proceeding with initial seeding of 20 books...");

        var allCategories = await ctx.Categories.ToListAsync();
        var allSellers = new[] { seller1, seller2, seller3 };
        var categoryDict = allCategories.ToDictionary(c => c.Name, c => c.Id);
        
        // Verify all required categories exist
        var requiredCategories = new[] { "Computer Science", "Mathematics", "Literature", "Biology", "Physics", "Business", "History" };
        var missingCategories = requiredCategories.Where(catName => !categoryDict.ContainsKey(catName)).ToList();
        if (missingCategories.Any())
        {
            Log.Error("Required categories are missing: {Categories}. Cannot seed listings.", string.Join(", ", missingCategories));
            throw new InvalidOperationException($"Required categories are missing: {string.Join(", ", missingCategories)}. Cannot seed listings.");
        }

        Log.Information("Seeding 20 new book listings...");

        var books = new List<Listing>
        {
            new Listing { Title = "Introduction to Algorithms (4th Edition)", Description = "Excellent condition, no highlights. Perfect for CS courses.", Price = 75.00m, Condition = "Like New", CategoryId = categoryDict["Computer Science"], SellerId = seller1.Id, IsSold = false },
            new Listing { Title = "Calculus: Early Transcendentals (8th Edition)", Description = "Clean copy with minimal notes. Includes access code.", Price = 85.00m, Condition = "Very Good", CategoryId = categoryDict["Mathematics"], SellerId = seller1.Id, IsSold = false },
            new Listing { Title = "The Great Gatsby", Description = "Classic literature, excellent for English courses. Well-preserved.", Price = 12.00m, Condition = "Good", CategoryId = categoryDict["Literature"], SellerId = seller1.Id, IsSold = false },
            new Listing { Title = "Campbell Biology (12th Edition)", Description = "Comprehensive biology textbook. Some highlighting.", Price = 95.00m, Condition = "Good", CategoryId = categoryDict["Biology"], SellerId = seller1.Id, IsSold = false },
            new Listing { Title = "University Physics with Modern Physics (15th Edition)", Description = "Heavy textbook but in great condition. No missing pages.", Price = 110.00m, Condition = "Very Good", CategoryId = categoryDict["Physics"], SellerId = seller1.Id, IsSold = false },
            new Listing { Title = "Clean Code: A Handbook of Agile Software Craftsmanship", Description = "Essential reading for developers. Slightly used.", Price = 35.00m, Condition = "Good", CategoryId = categoryDict["Computer Science"], SellerId = allSellers[1].Id, IsSold = false },
            new Listing { Title = "Linear Algebra and Its Applications (6th Edition)", Description = "Required for math majors. Clean pages, no writing.", Price = 65.00m, Condition = "Like New", CategoryId = categoryDict["Mathematics"], SellerId = allSellers[1].Id, IsSold = false },
            new Listing { Title = "To Kill a Mockingbird", Description = "Classic American literature. Good condition with minor wear.", Price = 10.00m, Condition = "Fair", CategoryId = categoryDict["Literature"], SellerId = allSellers[1].Id, IsSold = false },
            new Listing { Title = "Molecular Biology of the Cell (7th Edition)", Description = "Advanced biology text. Some notes in margins.", Price = 120.00m, Condition = "Good", CategoryId = categoryDict["Biology"], SellerId = allSellers[1].Id, IsSold = false },
            new Listing { Title = "Principles of Microeconomics (8th Edition)", Description = "Economics textbook. Good condition, no access code.", Price = 70.00m, Condition = "Very Good", CategoryId = categoryDict["Business"], SellerId = allSellers[2].Id, IsSold = false },
            new Listing { Title = "Design Patterns: Elements of Reusable Object-Oriented Software", Description = "Gang of Four book. Essential for software engineers.", Price = 45.00m, Condition = "Good", CategoryId = categoryDict["Computer Science"], SellerId = allSellers[2].Id, IsSold = false },
            new Listing { Title = "Discrete Mathematics and Its Applications (8th Edition)", Description = "Used for multiple CS courses. Well-maintained.", Price = 55.00m, Condition = "Very Good", CategoryId = categoryDict["Mathematics"], SellerId = allSellers[2].Id, IsSold = false },
            new Listing { Title = "1984 by George Orwell", Description = "Dystopian classic. Perfect condition.", Price = 8.00m, Condition = "Like New", CategoryId = categoryDict["Literature"], SellerId = allSellers[2].Id, IsSold = false },
            new Listing { Title = "Human Anatomy & Physiology (11th Edition)", Description = "Comprehensive anatomy book with diagrams intact.", Price = 100.00m, Condition = "Very Good", CategoryId = categoryDict["Biology"], SellerId = seller1.Id, IsSold = false },
            new Listing { Title = "Fundamentals of Physics (11th Edition Extended)", Description = "Complete physics textbook set. Excellent for physics majors.", Price = 105.00m, Condition = "Good", CategoryId = categoryDict["Physics"], SellerId = allSellers[1].Id, IsSold = false },
            new Listing { Title = "Data Structures and Algorithms in Java (6th Edition)", Description = "Java-focused algorithms book. Minimal wear.", Price = 60.00m, Condition = "Very Good", CategoryId = categoryDict["Computer Science"], SellerId = allSellers[2].Id, IsSold = false },
            new Listing { Title = "Statistical Methods for Psychology (9th Edition)", Description = "Statistics textbook. Some highlighting throughout.", Price = 50.00m, Condition = "Good", CategoryId = categoryDict["Mathematics"], SellerId = seller1.Id, IsSold = false },
            new Listing { Title = "The Catcher in the Rye", Description = "Classic coming-of-age novel. Good reading copy.", Price = 9.00m, Condition = "Fair", CategoryId = categoryDict["Literature"], SellerId = allSellers[1].Id, IsSold = false },
            new Listing { Title = "World Civilizations: The Global Experience (7th Edition)", Description = "Comprehensive world history textbook. Maps included.", Price = 80.00m, Condition = "Very Good", CategoryId = categoryDict["History"], SellerId = allSellers[2].Id, IsSold = false },
            new Listing { Title = "Operating System Concepts (10th Edition)", Description = "Silberschatz classic. Required for OS courses. Well-preserved.", Price = 90.00m, Condition = "Good", CategoryId = categoryDict["Computer Science"], SellerId = seller1.Id, IsSold = false }
        };

        ctx.Listings.AddRange(books);
        await ctx.SaveChangesAsync();

        Log.Information("Successfully seeded {Count} book listings", books.Count);
        Log.Information("Database seeding completed successfully.");
    }
}
