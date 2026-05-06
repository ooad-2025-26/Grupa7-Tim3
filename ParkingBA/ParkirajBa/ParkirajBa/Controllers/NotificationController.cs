using Microsoft.AspNetCore.Mvc;

namespace ParkirajBa.Controllers
{
    public class NotificationController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
