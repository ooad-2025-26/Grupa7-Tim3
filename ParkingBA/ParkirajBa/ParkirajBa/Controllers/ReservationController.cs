using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ParkirajBa.Data;
using ParkirajBa.Models;
using ParkirajBa.Repositories;

namespace ParkirajBa.Controllers
{
    [Authorize]
    public class ReservationController : Controller
    {
        private readonly ApplicationDbContext _database;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IParkingRepository _parkingRepository;

        public ReservationController(
            ApplicationDbContext database,
            UserManager<ApplicationUser> userManager,
            IParkingRepository parkingRepository)
        {
            _database = database;
            _userManager = userManager;
            _parkingRepository = parkingRepository;
        }

        // GET: /Reservation/Index
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return RedirectToAction("Login", "User");

            var rezervacije = await _database.Tickets
                .Include(t => t.ParkingObject)
                .Where(t => t.ApplicationUserId == user.Id)
                .OrderByDescending(t => t.IssuedAt)
                .ToListAsync();

            return View(rezervacije);
        }

        // GET: /Reservation/Create?parkingObjectId=1
        [HttpGet]
        public async Task<IActionResult> Create(int parkingObjectId)
        {
            var parking = await _parkingRepository.GetByIdWithPricingsAsync(parkingObjectId);

            if (parking == null)
            {
                ViewBag.Error = "Parking objekat nije pronađen.";
                return RedirectToAction("Objekti", "Home");
            }

            ViewBag.Parking = parking;
            return View();
        }

        // POST: /Reservation/Create
        [HttpPost]
        public async Task<IActionResult> Create(int parkingObjectId, DateTime expiresAt)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return RedirectToAction("Login", "User");

            var parking = await _parkingRepository.GetByIdWithPricingsAsync(parkingObjectId);

            if (parking == null)
            {
                ViewBag.Error = "Parking objekat nije pronađen.";
                return RedirectToAction("Objekti", "Home");
            }

            if (expiresAt <= DateTime.Now)
            {
                ViewBag.Error = "Datum isteka mora biti u budućnosti.";
                ViewBag.Parking = parking;
                return View();
            }

            // Using repository for getting hourly pricing rate
            var hourlyPricing = await _parkingRepository.GetActivePricingAsync(parkingObjectId, PricingType.Hourly);

            decimal cijena = 0;
            if (hourlyPricing != null)
            {
                double sati = (expiresAt - DateTime.Now).TotalHours;
                cijena = hourlyPricing.price * (decimal)Math.Ceiling(sati);
            }

            var ticket = new Ticket
            {
                ApplicationUserId = user.Id,
                ParkingObjectId = parkingObjectId,
                IssuedAt = DateTime.Now,
                ExpiresAt = expiresAt,
                Price = cijena
            };

            _database.Tickets.Add(ticket);
            await _database.SaveChangesAsync();

            return RedirectToAction("Details", new { id = ticket.Id });
        }

        // GET: /Reservation/Details/5
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return RedirectToAction("Login", "User");

            var ticket = await _database.Tickets
                .Include(t => t.ParkingObject)
                .FirstOrDefaultAsync(t => t.Id == id && t.ApplicationUserId == user.Id);

            if (ticket == null)
            {
                ViewBag.Error = "Rezervacija nije pronađena.";
                return RedirectToAction("Index");
            }

            return View(ticket);
        }

        // POST: /Reservation/Cancel/5
        [HttpPost]
        public async Task<IActionResult> Cancel(int id)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return RedirectToAction("Login", "User");

            var ticket = await _database.Tickets
                .FirstOrDefaultAsync(t => t.Id == id && t.ApplicationUserId == user.Id);

            if (ticket == null)
            {
                ViewBag.Error = "Rezervacija nije pronađena.";
                return RedirectToAction("Index");
            }

            if (ticket.ExpiresAt.HasValue && ticket.ExpiresAt < DateTime.Now)
            {
                ViewBag.Error = "Nije moguće otkazati isteklu rezervaciju.";
                return RedirectToAction("Index");
            }

            _database.Tickets.Remove(ticket);
            await _database.SaveChangesAsync();

            ViewBag.Success = "Rezervacija je uspješno otkazana.";
            return RedirectToAction("Index");
        }
    }
}