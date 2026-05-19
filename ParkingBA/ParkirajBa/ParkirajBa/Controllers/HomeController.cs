using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using ParkirajBa.Data;
using ParkirajBa.Models;
using System.Net;
using System.Net.Mail;

namespace ParkirajBa.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        private readonly ApplicationDbContext _database;

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext database)
        {
            _logger = logger;
            _database = database;
        }

        //db testing
        public IActionResult databaseTest()
        {
            ParkingObject parkingObject = new ParkingObject
            {
                name = "Test Parking",
                address = "Test Address",
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

        public IActionResult Index()
        {
            return View();
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
    }
}
