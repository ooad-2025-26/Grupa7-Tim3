using Microsoft.AspNetCore.Mvc;

namespace ParkirajBa.Controllers
{
    public class ParkingController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
