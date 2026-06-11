using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
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
        private readonly IEmailSender _emailSender;

        public ReservationController(
            ApplicationDbContext database,
            UserManager<ApplicationUser> userManager,
            IParkingRepository parkingRepository,
            IEmailSender emailSender)
        {
            _database = database;
            _userManager = userManager;
            _parkingRepository = parkingRepository;
            _emailSender = emailSender;
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
        public async Task<IActionResult> Create(int parkingObjectId, DateTime? startsAt, DateTime? expiresAt, string? pricingType)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "User");

            var parking = await _parkingRepository.GetByIdWithPricingsAsync(parkingObjectId);
            if (parking == null)
            {
                ViewBag.Error = "Parking objekat nije pronađen.";
                return RedirectToAction("Objekti", "Home");
            }

            if (!startsAt.HasValue)
            {
                ViewBag.Error = "Molimo unesite datum i vrijeme početka rezervacije.";
                ViewBag.Parking = parking;
                ViewBag.Pricings = await _parkingRepository.GetPricingsByParkingIdAsync(parkingObjectId) ?? new List<Pricing>();
                return View();
            }

            if (!expiresAt.HasValue)
            {
                ViewBag.Error = "Molimo unesite datum i vrijeme isteka rezervacije.";
                ViewBag.Parking = parking;
                ViewBag.Pricings = await _parkingRepository.GetPricingsByParkingIdAsync(parkingObjectId) ?? new List<Pricing>();
                return View();
            }

            if (startsAt.Value < DateTime.Now.AddMinutes(-5))
                startsAt = DateTime.Now;

            if (expiresAt.Value <= startsAt.Value)
            {
                ViewBag.Error = "Datum isteka mora biti nakon početka rezervacije.";
                ViewBag.Parking = parking;
                ViewBag.Pricings = await _parkingRepository.GetPricingsByParkingIdAsync(parkingObjectId) ?? new List<Pricing>();
                return View();
            }

            // resolve pricing type — default to Hourly
            PricingType selectedType = pricingType switch
            {
                "Daily" => PricingType.Daily,
                "Monthly" => PricingType.Monthly,
                "Yearly" => PricingType.Yearly,
                _ => PricingType.Hourly
            };

            var pricing = await _parkingRepository.GetActivePricingAsync(parkingObjectId, selectedType);

            // fallback to Hourly if selected type has no pricing defined
            if (pricing == null && selectedType != PricingType.Hourly)
                pricing = await _parkingRepository.GetActivePricingAsync(parkingObjectId, PricingType.Hourly);

            decimal cijena = 0;
            if (pricing != null)
            {
                var span = expiresAt.Value - startsAt.Value;
                cijena = selectedType switch
                {
                    PricingType.Hourly => pricing.price * (decimal)Math.Ceiling(span.TotalHours),
                    PricingType.Daily => pricing.price * (decimal)Math.Ceiling(span.TotalDays),
                    PricingType.Monthly => pricing.price * (decimal)Math.Max(1,
                        (expiresAt.Value.Year - startsAt.Value.Year) * 12 + expiresAt.Value.Month - startsAt.Value.Month),
                    PricingType.Yearly => pricing.price * (decimal)Math.Max(1,
                        expiresAt.Value.Year - startsAt.Value.Year),
                    _ => pricing.price * (decimal)Math.Ceiling(span.TotalHours)
                };
            }

            var ticket = new Ticket
            {
                ApplicationUserId = user.Id,
                ParkingObjectId = parkingObjectId,
                IssuedAt = startsAt.Value,
                ExpiresAt = expiresAt.Value,
                Price = cijena
            };

            _database.Tickets.Add(ticket);
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
        // GET: /Reservation/Extend/5
        [HttpGet]
        public async Task<IActionResult> Extend(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "User");

            var ticket = await _parkingRepository.GetTicketByIdAsync(id, user.Id);

            if (ticket == null || !ticket.IsPaid || ticket.IsCancelled)
            {
                TempData["Error"] = "Rezervacija nije pronađena ili nije dostupna za produženje.";
                return RedirectToAction("Rezervacije", "Home");
            }

            if (ticket.ExpiresAt.HasValue && ticket.ExpiresAt.Value < DateTime.Now)
            {
                TempData["Error"] = "Nije moguće produžiti isteklu rezervaciju.";
                return RedirectToAction("Details", new { id });
            }

            ViewBag.Ticket = ticket;
            ViewBag.Parking = ticket.ParkingObject;
            ViewBag.Pricings = await _parkingRepository.GetPricingsByParkingIdAsync(ticket.ParkingObjectId) ?? new List<Pricing>();
            return View();
        }

        // POST: /Reservation/Extend
        [HttpPost]
        public async Task<IActionResult> Extend(int id, int extraHours)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "User");

            var ticket = await _parkingRepository.GetTicketByIdAsync(id, user.Id);

            if (ticket == null || !ticket.IsPaid || ticket.IsCancelled)
            {
                TempData["Error"] = "Rezervacija nije pronađena.";
                return RedirectToAction("Rezervacije", "Home");
            }

            if (ticket.ExpiresAt.HasValue && ticket.ExpiresAt.Value < DateTime.Now)
            {
                TempData["Error"] = "Nije moguće produžiti isteklu rezervaciju.";
                return RedirectToAction("Details", new { id });
            }

            if (extraHours < 1 || extraHours > 24)
            {
                TempData["Error"] = "Broj sati mora biti između 1 i 24.";
                return RedirectToAction("Details", new { id });
            }

            var pricing = await _parkingRepository.GetActivePricingAsync(ticket.ParkingObjectId, PricingType.Hourly);
            decimal extraPrice = pricing != null ? pricing.price * extraHours : 0;

            // Kreirati novi ticket koji predstavlja produženje
            var extensionTicket = new Ticket
            {
                ApplicationUserId = user.Id,
                ParkingObjectId = ticket.ParkingObjectId,
                IssuedAt = ticket.ExpiresAt ?? DateTime.Now,
                ExpiresAt = (ticket.ExpiresAt ?? DateTime.Now).AddHours(extraHours),
                Price = extraPrice,
                // Označiti kao produženje — čuva vezu ka originalnom ticketu kroz ReservationCode prefix
                ReservationCode = null // generira se nakon plaćanja
            };

            // Privremeno sačuvati informaciju o produženju u session/TempData
            // Koristimo poseban TempData key da PaymentController zna da produžuje ExpiresAt originalnog ticketa
            TempData["ExtensionForTicketId"] = id;
            TempData["ExtensionHours"] = extraHours;
            TempData["ExtensionExpiresAt"] = extensionTicket.ExpiresAt?.ToString("o");

            _database.Tickets.Add(extensionTicket);
            await _database.SaveChangesAsync();

            return RedirectToAction("Checkout", "Payment", new { ticketId = extensionTicket.Id, isExtension = true, originalTicketId = id });
        }

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

            // Blokiranje otkazivanja ako je ulaz već evidentiran
            if (ticket.EnteredParking)
            {
                TempData["Error"] = "Nije moguće otkazati rezervaciju nakon što je ulazak evidentiran.";
                return RedirectToAction("Details", new { id });
            }

            string parkingName = ticket.ParkingObject?.name ?? "ParkirajBa Parking";
            string vrijediDo = ticket.ExpiresAt.HasValue
                ? ticket.ExpiresAt.Value.ToString("dd.MM.yyyy HH:mm") : "—";
            decimal cijena = ticket.Price;
            string korisnikIme = user.FullName ?? user.Email ?? "Korisnik";

            // Označi kao otkazana umjesto brisanja
            ticket.IsCancelled = true;
            ticket.QrCodeActive = false;

            var parking = await _parkingRepository.GetByIdAsync(ticket.ParkingObjectId);
            if (parking != null && ticket.IsPaid && parking.availableSpots < parking.totalSpots)
                parking.availableSpots++;

            await _database.SaveChangesAsync();

            try
            {
                await _emailSender.SendEmailAsync(
                    user.Email!,
                    "Otkazivanje rezervacije - ParkirajBa",
                    $"<p>Poštovani/a <strong>{korisnikIme}</strong>,</p>" +
                    $"<p>Vaša rezervacija je uspješno otkazana.</p>" +
                    $"<p><strong>Parking:</strong> {parkingName}<br/>" +
                    $"<strong>Iznos:</strong> {cijena:0.00} KM<br/>" +
                    $"<strong>Vrijedio do:</strong> {vrijediDo}</p>" +
                    $"<p>Sredstva će biti vraćena na vašu platnu karticu.</p>" +
                    $"<p>Hvala što koristite ParkirajBa!</p>"
                );
            }
            catch { }

            TempData["Success"] = "Rezervacija je uspješno otkazana.";
            return RedirectToAction("Rezervacije", "Home");
        }
    }
}
