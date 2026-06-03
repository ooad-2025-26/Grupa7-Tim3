using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
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
            ViewBag.Pricings = await _parkingRepository.GetPricingsByParkingIdAsync(parkingObjectId) ?? new List<Pricing>();
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(int parkingObjectId, DateTime startsAt, DateTime expiresAt)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "User");

            var parking = await _parkingRepository.GetByIdWithPricingsAsync(parkingObjectId);
            if (parking == null)
            {
                ViewBag.Error = "Parking objekat nije pronađen.";
                return RedirectToAction("Objekti", "Home");
            }

            if (startsAt < DateTime.Now.AddMinutes(-5))
                startsAt = DateTime.Now;

            if (expiresAt <= startsAt)
            {
                ViewBag.Error = "Datum isteka mora biti nakon početka rezervacije.";
                ViewBag.Parking = parking;
                ViewBag.Pricings = await _parkingRepository.GetPricingsByParkingIdAsync(parkingObjectId) ?? new List<Pricing>();
                return View();
            }

            var hourlyPricing = await _parkingRepository.GetActivePricingAsync(parkingObjectId, PricingType.Hourly);

            decimal cijena = 0;
            if (hourlyPricing != null)
            {
                double sati = (expiresAt - startsAt).TotalHours;
                cijena = hourlyPricing.price * (decimal)Math.Ceiling(sati);
            }

            var ticket = new Ticket
            {
                ApplicationUserId = user.Id,
                ParkingObjectId = parkingObjectId,
                IssuedAt = startsAt,
                ExpiresAt = expiresAt,
                Price = cijena
            };

            _database.Tickets.Add(ticket);

            // Smanji broj slobodnih mjesta
            if (parking.availableSpots > 0)
                parking.availableSpots--;

            await _database.SaveChangesAsync();

            return RedirectToAction("Checkout", "Payment", new { ticketId = ticket.Id });
        }

        // GET: /Reservation/Details/5
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "User");

            var ticket = await _parkingRepository.GetTicketByIdAsync(id, user.Id);

            if (ticket == null)
            {
                ViewBag.Error = "Rezervacija nije pronađena.";
                return RedirectToAction("Rezervacije", "Home");
            }

            return View(ticket);
        }

        // POST: /Reservation/Cancel
        [HttpPost]
        public async Task<IActionResult> Cancel(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "User");

            var ticket = await _parkingRepository.GetTicketByIdAsync(id, user.Id);

            if (ticket == null)
            {
                TempData["Error"] = "Rezervacija nije pronađena.";
                return RedirectToAction("Rezervacije", "Home");
            }

            if (ticket.ExpiresAt.HasValue && ticket.ExpiresAt < DateTime.Now)
            {
                TempData["Error"] = "Nije moguće otkazati isteklu rezervaciju.";
                return RedirectToAction("Details", new { id });
            }

            _database.Tickets.Remove(ticket);

            // free up parking slot
            var parking = await _parkingRepository.GetByIdAsync(ticket.ParkingObjectId);
            if (parking != null && parking.availableSpots < parking.totalSpots)
                parking.availableSpots++;

            await _database.SaveChangesAsync();

            TempData["Success"] = "Rezervacija je uspješno otkazana.";
            return RedirectToAction("Rezervacije", "Home");
        }
    }
}
