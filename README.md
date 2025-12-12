# Campus Marketplace - Student Textbook Trading Platform

## Overview

Campus Marketplace is a comprehensive full-stack web application designed specifically for college students to buy and sell used textbooks. Built with ASP.NET Core, this platform streamlines the process of finding affordable course materials while providing a secure, user-friendly marketplace for the campus community. The application features robust authentication, real-time messaging, advanced search capabilities, and an intuitive admin dashboard.

## Purpose

This project was developed as the capstone project for the Web Programming 3 course at John Abbott College. The goal was to create a production-ready web application that demonstrates mastery of modern web development practices, including clean architecture, design patterns, secure authentication, database management, and responsive front-end design.

## Key Features

### User Features

- **User Authentication & Authorization**: Secure JWT-based authentication system
- **User Profiles**: Customizable profiles with seller ratings and review history
- **Listing Management**: Create, edit, and delete textbook listings with photos
- **Advanced Search**: Filter by title, author, course, ISBN, price range, and condition
- **Real-time Messaging**: Built-in chat system for buyer-seller communication
- **Wishlist**: Save interesting listings for later
- **Transaction History**: Track all purchases and sales
- **Review System**: Rate and review transactions
- **Email Notifications**: Alerts for messages, offers, and listing updates

### Marketplace Features

- **Category Organization**: Browse by department, course, or semester
- **Condition Ratings**: New, Like New, Good, Fair, Poor
- **Price Comparison**: See market prices for the same textbook
- **Image Uploads**: Multiple photos per listing
- **Book Details**: ISBN, edition, publisher, author, course code
- **Seller Verification**: Campus email verification required
- **Report System**: Flag inappropriate listings or users

### Admin Dashboard

- **User Management**: View, suspend, or delete user accounts
- **Listing Moderation**: Review reported listings and remove violations
- **Analytics Dashboard**: Track platform usage and transactions
- **Category Management**: Add/edit/remove course categories
- **Bulk Operations**: Efficient management of multiple listings
- **Audit Logs**: Track all administrative actions

## Technologies Used

### Backend

- **ASP.NET Core 6.0**: Modern web framework
- **C# 10**: Primary programming language
- **Entity Framework Core**: ORM for database access
- **SQL Server**: Relational database
- **JWT Authentication**: Secure token-based auth
- **AutoMapper**: Object-to-object mapping
- **FluentValidation**: Model validation

### Architecture & Design Patterns

- **Repository Pattern**: Abstraction of data access logic
- **Unit of Work Pattern**: Transaction management
- **Dependency Injection**: Loose coupling and testability
- **Clean Architecture**: Separation of concerns
- **RESTful API Design**: Standard HTTP methods and status codes
- **MVC Pattern**: Model-View-Controller structure

### Frontend

- **Razor Pages**: Server-side rendering
- **Bootstrap 5**: Responsive CSS framework
- **JavaScript (ES6+)**: Client-side interactivity
- **jQuery**: DOM manipulation and AJAX
- **Font Awesome**: Icon library
- **Chart.js**: Data visualization for admin dashboard

### Development Tools

- **Visual Studio 2022**: IDE
- **Git**: Version control
- **Postman**: API testing
- **SQL Server Management Studio**: Database management
- **Azure**: Cloud hosting platform

## Installation & Setup

### Prerequisites

```bash
# .NET 6.0 SDK
dotnet --version

# SQL Server 2019 or later
# Visual Studio 2022 or VS Code
```

### Local Development Setup

```bash
# Clone the repository
git clone https://github.com/BalpreetSingh-code/Campus-Marketplace.git

# Navigate to project directory
cd Campus-Marketplace

# Restore NuGet packages
dotnet restore

# Update database connection string in appsettings.json
# Then apply migrations
dotnet ef database update

# Run the application
dotnet run

# Application will be available at:
# https://localhost:5001
```

### Database Setup

```bash
# Create database
dotnet ef migrations add InitialCreate
dotnet ef database update

# Seed sample data (optional)
dotnet run --seed
```

### Configuration

Update `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=CampusMarketplace;Trusted_Connection=True;"
  },
  "JwtSettings": {
    "Secret": "your-secret-key-here",
    "Issuer": "CampusMarketplace",
    "Audience": "CampusMarketplace",
    "ExpirationMinutes": 60
  },
  "EmailSettings": {
    "SmtpServer": "smtp.gmail.com",
    "SmtpPort": 587,
    "SenderEmail": "noreply@campusmarketplace.com",
    "SenderPassword": "your-password"
  }
}
```

## Project Structure

```
CampusMarketplace/
├── src/
│   ├── CampusMarketplace.Web/          # Main web application
│   │   ├── Controllers/                # API & MVC controllers
│   │   ├── Views/                      # Razor views
│   │   ├── wwwroot/                    # Static files
│   │   ├── Models/                     # View models
│   │   └── Program.cs                  # Application entry point
│   │
│   ├── CampusMarketplace.Core/         # Domain layer
│   │   ├── Entities/                   # Domain entities
│   │   ├── Interfaces/                 # Repository interfaces
│   │   ├── DTOs/                       # Data transfer objects
│   │   └── Specifications/             # Query specifications
│   │
│   ├── CampusMarketplace.Infrastructure/ # Data layer
│   │   ├── Data/                       # EF Core context
│   │   ├── Repositories/               # Repository implementations
│   │   ├── Services/                   # Business services
│   │   └── Migrations/                 # EF migrations
│   │
│   └── CampusMarketplace.Tests/        # Unit tests
│       ├── Controllers/
│       ├── Services/
│       └── Repositories/
│
├── docs/                               # Documentation
├── scripts/                            # Database scripts
└── README.md
```

## Database Schema

### Core Entities

#### User

```csharp
public class User
{
    public int Id { get; set; }
    public string Email { get; set; }
    public string PasswordHash { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string PhoneNumber { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsVerified { get; set; }
    public decimal SellerRating { get; set; }

    // Navigation properties
    public ICollection<Listing> Listings { get; set; }
    public ICollection<Message> SentMessages { get; set; }
    public ICollection<Message> ReceivedMessages { get; set; }
}
```

#### Listing

```csharp
public class Listing
{
    public int Id { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public string ISBN { get; set; }
    public string Author { get; set; }
    public string Publisher { get; set; }
    public int Edition { get; set; }
    public string CourseCode { get; set; }
    public decimal Price { get; set; }
    public BookCondition Condition { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsActive { get; set; }

    // Foreign keys
    public int UserId { get; set; }
    public int CategoryId { get; set; }

    // Navigation properties
    public User Seller { get; set; }
    public Category Category { get; set; }
    public ICollection<ListingImage> Images { get; set; }
    public ICollection<Transaction> Transactions { get; set; }
}
```

#### Message

```csharp
public class Message
{
    public int Id { get; set; }
    public string Content { get; set; }
    public DateTime SentAt { get; set; }
    public bool IsRead { get; set; }

    // Foreign keys
    public int SenderId { get; set; }
    public int ReceiverId { get; set; }
    public int ListingId { get; set; }

    // Navigation properties
    public User Sender { get; set; }
    public User Receiver { get; set; }
    public Listing Listing { get; set; }
}
```

## Repository Pattern Implementation

### Interface Definition

```csharp
public interface IRepository<T> where T : class
{
    Task<T> GetByIdAsync(int id);
    Task<IEnumerable<T>> GetAllAsync();
    Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate);
    Task AddAsync(T entity);
    void Update(T entity);
    void Remove(T entity);
}
```

### Generic Repository

```csharp
public class Repository<T> : IRepository<T> where T : class
{
    protected readonly ApplicationDbContext _context;
    private readonly DbSet<T> _dbSet;

    public Repository(ApplicationDbContext context)
    {
        _context = context;
        _dbSet = context.Set<T>();
    }

    public async Task<T> GetByIdAsync(int id)
    {
        return await _dbSet.FindAsync(id);
    }

    public async Task<IEnumerable<T>> GetAllAsync()
    {
        return await _dbSet.ToListAsync();
    }

    public async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate)
    {
        return await _dbSet.Where(predicate).ToListAsync();
    }

    public async Task AddAsync(T entity)
    {
        await _dbSet.AddAsync(entity);
    }

    public void Update(T entity)
    {
        _dbSet.Update(entity);
    }

    public void Remove(T entity)
    {
        _dbSet.Remove(entity);
    }
}
```

## Unit of Work Pattern

### Interface

```csharp
public interface IUnitOfWork : IDisposable
{
    IRepository<User> Users { get; }
    IRepository<Listing> Listings { get; }
    IRepository<Message> Messages { get; }
    IRepository<Transaction> Transactions { get; }

    Task<int> CompleteAsync();
}
```

### Implementation

```csharp
public class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _context;

    public IRepository<User> Users { get; private set; }
    public IRepository<Listing> Listings { get; private set; }
    public IRepository<Message> Messages { get; private set; }
    public IRepository<Transaction> Transactions { get; private set; }

    public UnitOfWork(ApplicationDbContext context)
    {
        _context = context;
        Users = new Repository<User>(_context);
        Listings = new Repository<Listing>(_context);
        Messages = new Repository<Message>(_context);
        Transactions = new Repository<Transaction>(_context);
    }

    public async Task<int> CompleteAsync()
    {
        return await _context.SaveChangesAsync();
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
```

## JWT Authentication Implementation

### Token Generation

```csharp
public class TokenService : ITokenService
{
    private readonly IConfiguration _configuration;

    public string GenerateToken(User user)
    {
        var securityKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_configuration["JwtSettings:Secret"])
        );

        var credentials = new SigningCredentials(
            securityKey,
            SecurityAlgorithms.HmacSha256
        );

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Name, $"{user.FirstName} {user.LastName}"),
            new Claim("IsVerified", user.IsVerified.ToString())
        };

        var token = new JwtSecurityToken(
            issuer: _configuration["JwtSettings:Issuer"],
            audience: _configuration["JwtSettings:Audience"],
            claims: claims,
            expires: DateTime.Now.AddMinutes(60),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
```

### Authentication Controller

```csharp
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITokenService _tokenService;

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterDto model)
    {
        // Validate model
        // Check if user exists
        // Hash password
        // Create user
        // Send verification email

        return Ok(new { message = "Registration successful" });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto model)
    {
        var user = await _unitOfWork.Users
            .FindAsync(u => u.Email == model.Email)
            .FirstOrDefaultAsync();

        if (user == null || !VerifyPassword(model.Password, user.PasswordHash))
            return Unauthorized();

        var token = _tokenService.GenerateToken(user);

        return Ok(new { token, user = new UserDto(user) });
    }
}
```

## API Endpoints

### Listings API

```
GET    /api/listings              # Get all listings
GET    /api/listings/{id}         # Get listing by ID
POST   /api/listings              # Create new listing
PUT    /api/listings/{id}         # Update listing
DELETE /api/listings/{id}         # Delete listing
GET    /api/listings/search       # Search listings
GET    /api/listings/user/{id}    # Get user's listings
```

### Messages API

```
GET    /api/messages              # Get user's messages
POST   /api/messages              # Send message
PUT    /api/messages/{id}/read    # Mark as read
DELETE /api/messages/{id}         # Delete message
```

### Users API

```
GET    /api/users/{id}            # Get user profile
PUT    /api/users/{id}            # Update profile
GET    /api/users/{id}/reviews    # Get user reviews
POST   /api/users/{id}/review     # Add review
```

## Data Seeding

### Comprehensive Seed Data

```csharp
public static class DbInitializer
{
    public static void Seed(ApplicationDbContext context)
    {
        // Seed categories
        var categories = new[]
        {
            new Category { Name = "Computer Science", Code = "CS" },
            new Category { Name = "Mathematics", Code = "MATH" },
            new Category { Name = "Physics", Code = "PHYS" },
            new Category { Name = "Chemistry", Code = "CHEM" },
            new Category { Name = "Biology", Code = "BIOL" }
        };

        context.Categories.AddRange(categories);

        // Seed users (with hashed passwords)
        var users = new[]
        {
            new User
            {
                Email = "john@college.edu",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123"),
                FirstName = "John",
                LastName = "Doe",
                IsVerified = true
            }
            // ... more users
        };

        context.Users.AddRange(users);

        // Seed listings
        var listings = new[]
        {
            new Listing
            {
                Title = "Introduction to Algorithms",
                ISBN = "978-0262033848",
                Author = "Cormen, Leiserson, Rivest, Stein",
                Price = 45.00m,
                Condition = BookCondition.Good,
                CourseCode = "CS201",
                CategoryId = 1,
                UserId = 1
            }
            // ... more listings
        };

        context.Listings.AddRange(listings);
        context.SaveChanges();
    }
}
```

## Security Features

### Input Validation

```csharp
public class CreateListingValidator : AbstractValidator<CreateListingDto>
{
    public CreateListingValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.Price)
            .GreaterThan(0)
            .LessThan(10000);

        RuleFor(x => x.ISBN)
            .Matches(@"^\d{13}$")
            .When(x => !string.IsNullOrEmpty(x.ISBN));
    }
}
```

### Password Security

```csharp
public class PasswordHasher
{
    public string HashPassword(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password, 12);
    }

    public bool VerifyPassword(string password, string hash)
    {
        return BCrypt.Net.BCrypt.Verify(password, hash);
    }
}
```

### SQL Injection Prevention

- Using parameterized queries via Entity Framework
- Input validation and sanitization
- Prepared statements for all database operations

### CSRF Protection

```csharp
services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";
});
```

## Learning Outcomes

### Web Development

- **ASP.NET Core MVC**: Building scalable web applications
- **Entity Framework Core**: Code-first database approach
- **RESTful API Design**: Creating well-structured APIs
- **Authentication & Authorization**: Implementing secure user systems
- **Razor Pages**: Server-side rendering techniques

### Software Architecture

- **Repository Pattern**: Abstracting data access
- **Unit of Work**: Managing transactions
- **Dependency Injection**: Achieving loose coupling
- **Clean Architecture**: Organizing code effectively
- **Design Patterns**: Applying proven solutions

### Database Management

- **SQL Server**: Relational database design
- **Migrations**: Version control for database schema
- **Query Optimization**: Writing efficient queries
- **Data Modeling**: Creating normalized schemas
- **Seeding**: Populating test data

## Testing

### Unit Tests

```csharp
public class ListingServiceTests
{
    [Fact]
    public async Task CreateListing_ValidData_ReturnsListing()
    {
        // Arrange
        var mockUnitOfWork = new Mock<IUnitOfWork>();
        var service = new ListingService(mockUnitOfWork.Object);

        var dto = new CreateListingDto
        {
            Title = "Test Book",
            Price = 25.00m
        };

        // Act
        var result = await service.CreateListingAsync(dto, userId: 1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Test Book", result.Title);
    }
}
```

## Deployment

### Azure App Service

```bash
# Publish to Azure
dotnet publish -c Release

# Deploy using Azure CLI
az webapp up --name campus-marketplace --resource-group myResourceGroup
```

### Database Migration

```bash
# Update production database
dotnet ef database update --connection "production-connection-string"
```

## Challenges & Solutions

### Challenge 1: Image Upload Management

**Problem**: Handling multiple image uploads efficiently
**Solution**: Implemented async file upload with image compression and Azure Blob Storage

### Challenge 2: Real-time Messaging

**Problem**: Implementing instant message delivery
**Solution**: Used SignalR for WebSocket-based real-time communication

### Challenge 3: Search Performance

**Problem**: Slow full-text search on large dataset
**Solution**: Implemented database indexing and caching strategy

## Future Enhancements

- [ ] Mobile app (React Native or Flutter)
- [ ] Payment gateway integration (Stripe/PayPal)
- [ ] Advanced analytics dashboard
- [ ] Machine learning for price recommendations
- [ ] Book condition assessment via image recognition
- [ ] Integration with campus LMS
- [ ] Auction-style listings
- [ ] Textbook rental system
- [ ] API for third-party integrations

## Performance Optimization

- **Database Indexing**: Key columns indexed for faster queries
- **Caching**: Redis for frequently accessed data
- **Lazy Loading**: Efficient data retrieval with EF Core
- **CDN**: Static assets served via CDN
- **Compression**: Gzip compression enabled

## Contributors

**Developer**: Balpreet Singh Sahota  
**Role**: Full-stack development, architecture design, database design

## Acknowledgments

### Educational Institution

John Abbott College - Web Programming 3 Course

### Technologies & Libraries

- ASP.NET Core Team at Microsoft
- Entity Framework Core documentation
- Bootstrap framework
- JWT.io resources

## License

Educational project for academic purposes.

## Contact

**Developer**: Balpreet Singh Sahota  
**Institution**: John Abbott College - Computer Science  
**Course**: Web Programming 3  
**GitHub**: [@BalpreetSingh-code](https://github.com/BalpreetSingh-code)  
**Email**: sahotabalpreetsingh1@gmail.com

**Repository**: https://github.com/BalpreetSingh-code/Campus-Marketplace

---

_Connecting students with affordable textbooks | Built with ASP.NET Core_
