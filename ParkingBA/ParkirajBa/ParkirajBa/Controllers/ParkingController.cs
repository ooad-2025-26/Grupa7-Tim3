using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ParkirajBa.Models;
using ParkirajBa.Repositories;

namespace ParkirajBa.Controllers
{
    [Authorize(Roles = "Owner")]
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

        public async Task<IActionResult> ParkingManagement()
        {
            var currentUser = await _userManager.GetUserAsync(User);

            if (currentUser == null)
            {
                return Unauthorized();
            }

            var parkingObjects = await _parkingRepository.GetByOwnerIdAsync(currentUser.Id);

            return View(parkingObjects);
        }
    }
}
