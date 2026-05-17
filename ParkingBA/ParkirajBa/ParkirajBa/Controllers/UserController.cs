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
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();

            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult Login()
        {
            ViewBag.HideHeader = true;
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
            ViewBag.HideHeader = true;
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

        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return RedirectToAction("Login");
            }

            return View(user);
        }

        [HttpPost]
        public async Task<IActionResult> Profile(
       string firstName,
       string lastName,
       string currentPassword,
       string newPassword,
       string confirmPassword)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return RedirectToAction("Login");
            }

            user.FirstName = firstName;
            user.LastName = lastName;

            var updateResult = await _userManager.UpdateAsync(user);

            if (!updateResult.Succeeded)
            {
                ViewBag.Error = "Greška pri ažuriranju profila";
                return View(user);
            }

            // promjena passworda
            if (!string.IsNullOrWhiteSpace(newPassword))
            {
                if (newPassword != confirmPassword)
                {
                    ViewBag.Error = "Novi passwordi se ne poklapaju";
                    return View(user);
                }

                var passwordResult = await _userManager.ChangePasswordAsync(
                    user,
                    currentPassword,
                    newPassword);

                if (!passwordResult.Succeeded)
                {
                    ViewBag.Error = string.Join(", ",
                        passwordResult.Errors.Select(e => e.Description));

                    return View(user);
                }
            }

            ViewBag.Success = "Profil uspješno ažuriran";

            return View(user);
        }

        //Registracija vlasnika parkinga
        [HttpGet]
        public IActionResult RegisterOwner()
        {
            ViewBag.HideHeader = true;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> RegisterOwner(
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
                ViewBag.Error = string.Join(", ",
                    result.Errors.Select(e => e.Description));

                return View();
            }

            // dodavanje role - Owner
            await _userManager.AddToRoleAsync(user, "Owner");

            await _signInManager.SignInAsync(user, false);

            return RedirectToAction("Index", "Home");
        }

        //--------------------------------
        //ispis svih iz baze za testiranje
        public async Task<IActionResult> TestUsers()
        {
            var users = _userManager.Users.ToList();
            return Json(users);
        }
    }
}
