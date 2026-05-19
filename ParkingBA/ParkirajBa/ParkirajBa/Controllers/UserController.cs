using Microsoft.AspNetCore.Mvc;
using ParkirajBa.Data;
using ParkirajBa.Models;
namespace ParkirajBa.Controllers
{

    public class UserController : Controller
    {
        private readonly ApplicationDbContext _database;
        public UserController(ApplicationDbContext database)
        {
            _database = database;
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        // POST - login form
        [HttpPost]
        public IActionResult Login(string email, string password)
        {
            
            if (email == "dmuharem@etf.unsa.ba" && password == "123")
            {
               
                return RedirectToAction("Index", "Home");
            }
            ViewBag.Error = "Pogrešan email ili password";
            return View();
        }

        
    }
}
