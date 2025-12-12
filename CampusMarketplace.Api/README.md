## Project Overview – Campus Marketplace

**Campus Marketplace** is a secure and modern **ASP.NET Core Web API** that enables students to **buy and sell used textbooks** within their college community.

The system supports three user roles — **Admin**, **Seller**, and **Buyer** — each with specific permissions and workflows:

- **Sellers** can create, edit, and manage book listings.  
- **Buyers** can browse listings, make offers, and place direct orders.  
- Both roles can leave **reviews** after successful transactions, building trust across the platform.

Built using **Entity Framework Core (Code-First)**, the backend follows the **Repository** and **Unit of Work** design patterns for clean, maintainable, and testable data access.  
Comprehensive **logging** is implemented with **Serilog**, capturing key application events such as user authentication, listing creation, and order status updates.

To facilitate testing and demonstrations, the database is **automatically seeded** with sample users, categories, and listings upon startup.
