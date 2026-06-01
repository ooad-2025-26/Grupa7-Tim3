using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ParkirajBa.Models;
using ParkirajBa.Repositories;

namespace ParkirajBa.Controllers
{
    public class ParkingController : Controller
    {
        private readonly IParkingRepository _parkingRepository;
        private readonly UserManager<ApplicationUser> _userManager;

        public ParkingController(
            IParkingRepository parkingRepository,
            UserManager<ApplicationUser> userManager)
        {
            _parkingRepository = parkingRepository;
            _userManager = userManager;
        }

        // GET: /Parking/Details/5 — everyone
        public async Task<IActionResult> Details(int id)
        {
            var parking = await _parkingRepository.GetByIdAsync(id);

            if (parking == null)
                return RedirectToAction("Objekti", "Home");

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
    }
}
