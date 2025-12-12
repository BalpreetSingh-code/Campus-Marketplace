using CampusMarketplace.Api.Data;
using CampusMarketplace.Api.Models;
using CampusMarketplace.Api.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Extensions.FileSystemGlobbing.Internal;
using System;
using System.Threading.Channels;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace CampusMarketplace.Api.Repositories;

// Coordinates all repositories and ensures all database changes are saved together in one transaction
// This prevents partial updates - either all changes save successfully or none do
public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _ctx;

    public UnitOfWork(
        AppDbContext ctx,
        ICategoryRepository categories,
        IListingRepository listings,
        IOfferRepository offers,
        IOrderRepository orders,
        IReviewRepository reviews,
        IUserRepository users)
    {
        _ctx = ctx;
        Categories = categories;
        Listings = listings;
        Offers = offers;
        Orders = orders;
        Reviews = reviews;
        Users = users;
    }

    public ICategoryRepository Categories { get; }
    public IListingRepository Listings { get; }
    public IOfferRepository Offers { get; }
    public IOrderRepository Orders { get; }
    public IReviewRepository Reviews { get; }
    public IUserRepository Users { get; }

    // Save all pending changes from all repositories in one transaction
    public Task<int> SaveAsync() => _ctx.SaveChangesAsync();
}
