using Microsoft.AspNetCore.Mvc;

namespace ParkirajBa.Controllers
{
    public class ReportController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
