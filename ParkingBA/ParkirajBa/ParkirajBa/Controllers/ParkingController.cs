using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ParkirajBa.Data;
using ParkirajBa.Models;
using ParkirajBa.Repositories;

namespace ParkirajBa.Controllers
{
    public class ParkingController : Controller
    {
        private readonly IParkingRepository _parkingRepository;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _database;

        public ParkingController(
            IParkingRepository parkingRepository,
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext database)
        {
            _parkingRepository = parkingRepository;
            _userManager = userManager;
            _database = database;
        }

        // GET: /Parking/Details/5 — everyone
        public async Task<IActionResult> Details(int id)
        {
            var parking = await _parkingRepository.GetByIdAsync(id);

            if (parking == null)
            {
                Console.WriteLine("Id is " + id);
                return RedirectToAction("Objekti", "Home");
            }
            var pricings = await _parkingRepository.GetPricingsByParkingIdAsync(id);
            ViewBag.Pricings = pricings;

            return View(parking);
        }

        // GET: /Parking/ParkingManagement — Owner only
        [Authorize(Roles = "Owner")]
        public async Task<IActionResult> ParkingManagement()
        {
            var currentUser = await _userManager.GetUserAsync(User);

            if (currentUser == null)
                return Unauthorized();

            var parkingObjects = await _parkingRepository.GetByOwnerIdAsync(currentUser.Id);

            return View(parkingObjects);
        }

        // GET: /Parking/Edit/5 — Owner or Admin
        [Authorize(Roles = "Owner,Admin")]
        public async Task<IActionResult> Edit(int id)
        {
            var parking = await _parkingRepository.GetByIdWithPricingsAsync(id);
            if (parking == null) return NotFound();

            if (User.IsInRole("Owner"))
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if (parking.OwnerId != currentUser?.Id)
                    return Forbid();
            }

            return View(parking);
        }

        // POST: /Parking/Edit/5
        [HttpPost]
        [Authorize(Roles = "Owner,Admin")]
        public async Task<IActionResult> Edit(int id, string name, string address,
            int totalSpots, bool hasCameras, bool isDisabledAccessible,
            bool hasEVCharger, bool isUnderground, double? maxHeight,
            List<int>? pricingIds, List<decimal>? pricingValues)
        {
            var parking = await _parkingRepository.GetByIdAsync(id);
            if (parking == null) return NotFound();

            // owner can edit only their own parking, admin can edit all
            if (User.IsInRole("Owner"))
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if (parking.OwnerId != currentUser?.Id)
                    return Forbid();
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                ViewBag.Error = "Naziv parkinga je obavezan.";
                return View(parking);
            }

            if (totalSpots < 1)
            {
                ViewBag.Error = "Ukupan broj mjesta mora biti veći od 0.";
                return View(parking);
            }

            // validate prices server-side
            if (pricingValues != null && pricingValues.Any(p => p < 0))
            {
                ViewBag.Error = "Cijena ne može biti negativna.";
                ViewBag.Pricings = await _parkingRepository.GetPricingsByParkingIdAsync(id);
                return View("~/Views/Admin/ParkingDetails.cshtml", parking);
            }

            int diff = totalSpots - (parking.totalSpots ?? 0);
            parking.name = name;
            parking.address = address;
            parking.totalSpots = totalSpots;
            parking.availableSpots = Math.Max(0, parking.availableSpots + diff);
            parking.hasCameras = hasCameras;
            parking.isDisabledAccessible = isDisabledAccessible;
            parking.hasEVCharger = hasEVCharger;
            parking.isUnderground = isUnderground;
            parking.maxHeight = maxHeight;

            _database.ParkingObject.Update(parking);

            // update pricing rows
            if (pricingIds != null && pricingValues != null)
            {
                for (int i = 0; i < pricingIds.Count && i < pricingValues.Count; i++)
                {
                    var pricing = await _database.Pricing.FindAsync(pricingIds[i]);
                    if (pricing != null && pricing.ParkingObjectID == id)
                    {
                        pricing.price = pricingValues[i];
                        _database.Pricing.Update(pricing);
                    }
                }
            }

            await _database.SaveChangesAsync();

            TempData["Success"] = "Parking je uspješno ažuriran.";

            // redirect
            if (User.IsInRole("Admin"))
                return RedirectToAction("ParkingDetails", "Admin", new { id });

            return RedirectToAction("ParkingManagement");
        }
    }
}