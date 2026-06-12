using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ParkirajBa.Data;
using ParkirajBa.Models;
using ParkirajBa.Repositories;
using System.Diagnostics;
using System.Net;
using System.Net.Mail;

namespace ParkirajBa.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _database;
        private readonly IConfiguration _configuration;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IParkingRepository _parkingRepository;

        public HomeController(
            ILogger<HomeController> logger,
            ApplicationDbContext database,
            IConfiguration configuration,
            UserManager<ApplicationUser> userManager,
            IParkingRepository parkingRepository)
        {
            _logger = logger;
            _database = database;
            _configuration = configuration;
            _userManager = userManager;
            _parkingRepository = parkingRepository;
        }

        // db testing
        public IActionResult databaseTest()
        {
            ParkingObject parkingObject = new ParkingObject
            {
                name = "Test Parking 2",
                address = "Test Address 2",
                latitude = 44.7866,
                longitude = 17.4489,
                totalSpots = 100,
                hasCameras = true,
                isDisabledAccessible = true,
                hasEVCharger = false,
                maxHeight = 2.5,
                isUnderground = false,
                opensAt = DateTime.Parse("08:00"),
                closesAt = DateTime.Parse("22:00")
            };

            _database.ParkingObject.Add(parkingObject);
            _database.SaveChanges();

            Pricing pricing = new Pricing
            {
                pricingType = PricingType.Hourly,
                price = 2.5m,
                validFrom = DateTime.Now,
                validTo = DateTime.Now.AddMonths(1),
                ParkingObjectID = parkingObject.ID
            };
            _database.Pricing.Add(pricing);
            _database.SaveChanges();

            return Content("Database test completed successfully!");
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user != null && await _userManager.IsInRoleAsync(user, "Admin"))
                return RedirectToAction("Dashboard", "Admin");

            ViewBag.FullName = await GetFullNameAsync();
            ViewData["GoogleMapsApiKey"] = _configuration["GoogleMaps:ApiKey"];

            var parkingObjekti = await _parkingRepository.GetAllApprovedAsync();

            return View(parkingObjekti);
        }

        // objects tab
        public async Task<IActionResult> Objekti()
        {
            ViewBag.FullName = await GetFullNameAsync();

            var objekti = await _parkingRepository.GetAllApprovedAsync();

            // Dohvati primarne slike za svaki parking
            var slike = new Dictionary<int, string>();
            foreach (var parking in objekti)
            {
                var slika = await _parkingRepository.GetPrimaryImageByParkingIDAsync(parking.ID);
                slike[parking.ID] = slika?.ImagePath ?? "/images/parking.png";
            }
            ViewBag.Slike = slike;

            return View(objekti);
        }

        // reservation tab
        [Authorize]
        public async Task<IActionResult> Rezervacije()
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

        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }

        public IActionResult Privacy() => View();

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error() => View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });

        public IActionResult Login() => View();
        public IActionResult Register() => View();
        public IActionResult Placanje() => View();

        public IActionResult Potvrda(string ime)
        {
            ViewBag.ImeKorisnika = ime;
            return View();
        }

        public IActionResult Uspjeh() => View();

        [HttpPost]
        public async Task<IActionResult> PosaljiEmail(string ime)
        {
            try
            {
                string userEmail = User.Identity.Name;

                if (string.IsNullOrEmpty(userEmail))
                    return RedirectToAction("Login");

                var parkingBaEmail = "parkirajba.service@gmail.com";
                var parkingBaEmailConnection = "iplx fham rnwz oajz";

                var smtpClient = new SmtpClient("smtp.gmail.com")
                {
                    Port = 587,
                    Credentials = new NetworkCredential(parkingBaEmail, parkingBaEmailConnection),
                    EnableSsl = true,
                };

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(parkingBaEmail, "ParkirajBa"),
                    Subject = "Potvrda rezervacije - ParkirajBa",
                    Body = $"Poštovani {ime},\n\nVaša rezervacija je uspješno potvrđena! Hvala Vam što koristite ParkirajBa.",
                    IsBodyHtml = false,
                };

                mailMessage.To.Add(userEmail);
                await smtpClient.SendMailAsync(mailMessage);

                return RedirectToAction("Uspjeh");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Greška pri slanju emaila");
                return Content("Greška: " + ex.Message);
            }
        }

        // ── Helper

        private async Task<string> GetFullNameAsync()
        {
            if (!User.Identity.IsAuthenticated)
                return "Guest";

            var user = await _database.Users
                .FirstOrDefaultAsync(u => u.UserName == User.Identity.Name);

            return user != null ? user.FirstName + " " + user.LastName : "Guest";
        }

        //-- Map --
        [HttpGet]
        public async Task<IActionResult> Search(string searchText, bool hasGarage, bool hasEVCharger, bool hasCameras, bool isDisabledAccessible, string regime, int maxPrice)
        {
            // Execute query and fetch data
            var results =
                await _parkingRepository.FilterApprovedParkings(searchText, hasGarage, hasEVCharger, hasCameras, isDisabledAccessible, regime, maxPrice);

            // Return filtered results as JSON to the frontend
            return Json(results);
        }

        [HttpGet]
        public async Task<decimal> GetMaxPriceForRegime(PricingType type)
        {
            return await _parkingRepository.GetMaxPricingForRegimeAsync(type);
        }

        //----

        //-- Parking Image --
        [HttpGet]
        public async Task<IActionResult> GetPrimaryParkingImage(int parkingID)
        {
            return Json(await _parkingRepository.GetPrimaryImageByParkingIDAsync(parkingID));
        }

        //----
    }
}