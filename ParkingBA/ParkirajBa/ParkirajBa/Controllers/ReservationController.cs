using Microsoft.AspNetCore.Mvc;

namespace ParkirajBa.Controllers
{
    public class ReservationController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
