using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ParkirajBa.Data;
using ParkirajBa.Models;
namespace ParkirajBa.Controllers
{

    public class UserController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;

        public UserController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(string email, string password)
        {
            var user = await _userManager.FindByEmailAsync(email);

            if (user == null)
            {
                ViewBag.Error = "Korisnik ne postoji";
                return View();
            }

            var result = await _signInManager.PasswordSignInAsync(
                user.UserName,
                password,
                isPersistent: false,
                lockoutOnFailure: false);

            if (result.Succeeded)
            {
                return RedirectToAction("Index", "Home");
            }

            ViewBag.Error = "Pogrešan password";
            return View();
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(
        string ime,
        string prezime,
        string email,
        string password,
        string confirmPassword)
        {
            if (password != confirmPassword)
            {
                ViewBag.Error = "Passwordi se ne poklapaju";
                return View();
            }

            var postoji = await _userManager.FindByEmailAsync(email);
            if (postoji != null)
            {
                ViewBag.Error = "Korisnik već postoji";
                return View();
            }

            var user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                FirstName = ime,
                LastName = prezime
            };

            var result = await _userManager.CreateAsync(user, password);

            if (!result.Succeeded)
            {
                ViewBag.Error = string.Join(", ", result.Errors.Select(e => e.Description));
                return View();
            }

            await _userManager.AddToRoleAsync(user, "User");

            await _signInManager.SignInAsync(user, isPersistent: false);

            return RedirectToAction("Index", "Home");
        }


        //ispis svih iz baze za testiranje
        public async Task<IActionResult> TestUsers()
        {
            var users = _userManager.Users.ToList();
            return Json(users);
        }
    }
}
