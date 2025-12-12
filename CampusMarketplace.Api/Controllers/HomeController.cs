using CampusMarketplace.Api.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CampusMarketplace.Api.Controllers
{
    public class HomeController : Controller
    {
        private readonly IUnitOfWork _uow;

        public HomeController(IUnitOfWork uow)
        {
            _uow = uow;
        }

        [AllowAnonymous]
        public async Task<IActionResult> Index()
        {
            // Get recent listings for the landing page (excluding sold listings)
            var allListings = await _uow.Listings.GetAllWithDetailsAsync();
            var recentListings = allListings
                .Where(l => !l.IsSold) // Only show available listings
                .OrderByDescending(l => l.Id)
                .Take(6)
                .ToList();

            ViewData["RecentListings"] = recentListings;
            return View();
        }
    }
}
