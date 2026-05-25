using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ParkirajBa.Data;
using ParkirajBa.Models;

namespace ParkirajBa.Controllers
{
    [Authorize(Roles = "Owner")]
    public class ParkingController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ParkingController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> ParkingManagement()
        {
            var currentUser = await _userManager.GetUserAsync(User);

            if (currentUser == null)
            {
                return Unauthorized();
            }

            var parkingObject = await _context.ParkingObject
                .Where(p => p.OwnerId == currentUser.Id)
                .ToListAsync();

            return View(parkingObject);
        }
    }
}