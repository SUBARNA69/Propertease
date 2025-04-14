using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;
using System.Net.Http;
using Propertease.Models;
using Microsoft.EntityFrameworkCore;

namespace Propertease.Controllers
{
    public class RecommendationController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ProperteaseDbContext _context;

        public RecommendationController(IHttpClientFactory httpClientFactory, ProperteaseDbContext context)
        {
            _httpClientFactory = httpClientFactory;
            _context = context;
        }

        [HttpGet]
        public IActionResult Similar(int id)
        {
            // Show a form or directly fetch based on existing property
            // For simplicity, redirect to POST
            return RedirectToAction(nameof(SimilarPost), new { id });
        }

        [HttpPost]
        public async Task<IActionResult> SimilarPost(int id)
        {
            // 1. Fetch the source house record
            var house = await _context.Houses
                .Include(h => h.Properties)
                .FirstOrDefaultAsync(h => h.PropertyID == id);

            if (house == null) return NotFound();

            // 2. Prepare payload for Flask
            var payload = new
            {
                Area = house.BuildupArea,
                Bedrooms = house.Bedrooms,
                BuiltYear = house.BuiltYear,
                Floors = house.Floors
            };
            var json = JsonSerializer.Serialize(payload);
            var client = _httpClientFactory.CreateClient();
            var resp = await client.PostAsync(
                "http://localhost:5000/recommend",
                new StringContent(json, Encoding.UTF8, "application/json")
            );

            if (!resp.IsSuccessStatusCode)
            {
                // handle error
                ModelState.AddModelError("", "Recommendation service unavailable");
                return View(new List<SimilarPropertyViewModel>());
            }

            var idList = JsonSerializer.Deserialize<List<int>>(await resp.Content.ReadAsStringAsync());

            // 3. Query your DB for those properties
            var similarHouses = await _context.Houses
                .Include(h => h.Properties)
                .Where(h => idList.Contains(h.PropertyID))
                .ToListAsync();

            // 4. Map to view model
            var vm = similarHouses.Select(h => new SimilarPropertyViewModel
            {
                PropertyId = h.PropertyID,
                Title = h.Properties.Title,
                Price = h.Properties.Price,
                Bedrooms = h.Bedrooms,
                Bathrooms = h.Bathrooms,
                BuildupArea = h.BuildupArea,
                Floors = h.Floors,
                ImageUrl = h.Properties.PropertyImages
                             .Select(img => "/Images/" + img.Photo)
                             .FirstOrDefault() ?? "/placeholder.svg"
            }).ToList();

            return View(vm);
        }
    }
}
