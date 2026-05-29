using Microsoft.AspNetCore.Authentication;
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

        private readonly IParkingRepository _ParkingRepository;

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext database, IConfiguration configuration, IParkingRepository parkingRepository)
        {
            _logger = logger;
            _database = database;
            _configuration = configuration;
            _ParkingRepository = parkingRepository;
        }

        //db testing
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
            string fullName = "Guest";

            if (User.Identity.IsAuthenticated)
            {
                var user = await _database.Users
                    .FirstOrDefaultAsync(u => u.UserName == User.Identity.Name);

                if (user != null)
                {
                    fullName = user.FirstName + " " + user.LastName;
                }
            }

            ViewBag.FullName = fullName;

            //-- Mapa --

            var apiKey = _configuration["GoogleMaps:ApiKey"];

            ViewData["GoogleMapsApiKey"] = apiKey;

            var parkingObjekti = _database.ParkingObject.ToList();

            //----

            return View(parkingObjekti);
        }

        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        public IActionResult Login()
        {
            return View();
        }

        public IActionResult Register()
        {
            return View();
        }


        public IActionResult Placanje()
        {
            return View();
        }
        public IActionResult Potvrda(string ime)
        {
            ViewBag.ImeKorisnika = ime;
            return View();
        }
        public IActionResult Uspjeh()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> PosaljiEmail(string ime)
        {
            try
            {
                string userEmail = User.Identity.Name;

                if (string.IsNullOrEmpty(userEmail))
                {
                    return RedirectToAction("Login");
                }

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

        //Objekti Tab
        public async Task<IActionResult> Objekti()
        {
            string fullName = "Guest";

            if (User.Identity.IsAuthenticated)
            {
                var user = await _database.Users
                    .FirstOrDefaultAsync(u => u.UserName == User.Identity.Name);

                if (user != null)
                {
                    fullName = user.FirstName + " " + user.LastName;
                }
            }

            ViewBag.FullName = fullName;

            var objekti = await _database.ParkingObject.ToListAsync();

            return View(objekti);
        }

        //Rezervacije tab
        public IActionResult Rezervacije()
        {
            return View();
        }


        //-- Mapa --
        [HttpGet]
        public async Task<IActionResult> Search(string searchText, bool hasGarage, bool hasEVCharger, bool hasCameras,bool isDisabledAccessible,string regime, int maxPrice)
        {
            // Execute query and fetch data
            var results = 
                await _ParkingRepository.FilterParkings(searchText,hasGarage,hasEVCharger,hasCameras, isDisabledAccessible, regime, maxPrice);

            // Return filtered results as JSON to the frontend
            return Json(results);
        }


        //----
    }
}
