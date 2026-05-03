using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using ParkirajBa.Data;
using ParkirajBa.Models;

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

        //testiranje baze
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
    }
}
