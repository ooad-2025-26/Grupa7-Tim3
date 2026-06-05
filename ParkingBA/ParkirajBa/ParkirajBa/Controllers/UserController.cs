using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using ParkirajBa.Models;

namespace ParkirajBa.Controllers
{
    public class UserController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IEmailSender _emailSender;

        public UserController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IEmailSender emailSender)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _emailSender = emailSender;
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
                ViewBag.Error = "Korisnik ne postoji.";
                return View();
            }

            // Provjeri je li email potvrđen
            if (!await _userManager.IsEmailConfirmedAsync(user))
            {
                ViewBag.Error = "Molimo potvrdite email adresu. Provjerite inbox.";
                ViewBag.ShowResend = true;
                ViewBag.UserId = user.Id;
                return View();
            }

            var result = await _signInManager.PasswordSignInAsync(
                user.UserName, password, isPersistent: false, lockoutOnFailure: false);

            if (result.Succeeded)
            {
                if (await _userManager.IsInRoleAsync(user, "Admin"))
                    return RedirectToAction("Dashboard", "Admin");

                return RedirectToAction("Index", "Home");
            }

            ViewBag.Error = "Pogrešan password.";
            return View();
        }

        // GET: /User/ResendConfirmation?userId=...
        [HttpGet]
        public async Task<IActionResult> ResendConfirmation(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return RedirectToAction("Login");

            await SendConfirmationEmailAsync(user);

            ViewBag.Message = "Email potvrde je ponovo poslan. Provjerite inbox.";
            return View("EmailConfirmationSent");
        }

        [HttpGet]
        public IActionResult Register()
        {
            ViewBag.HideHeader = true;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(
            string ime, string prezime, string email,
            string password, string confirmPassword)
        {
            ViewBag.HideHeader = true;

            var nameRegex = new System.Text.RegularExpressions.Regex(@"^[a-zA-ZčćžšđČĆŽŠĐ\s\-]+$");

            if (string.IsNullOrWhiteSpace(ime) || !nameRegex.IsMatch(ime))
            {
                ViewBag.Error = "Ime može sadržavati samo slova, razmak i crticu.";
                return View();
            }

            if (string.IsNullOrWhiteSpace(prezime) || !nameRegex.IsMatch(prezime))
            {
                ViewBag.Error = "Prezime može sadržavati samo slova, razmak i crticu.";
                return View();
            }

            if (password != confirmPassword)
            {
                ViewBag.Error = "Passwordi se ne poklapaju.";
                return View();
            }

            var postoji = await _userManager.FindByEmailAsync(email);
            if (postoji != null)
            {
                ViewBag.Error = "Korisnik već postoji.";
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

            // Pošalji email potvrdu
            await SendConfirmationEmailAsync(user);

            return View("EmailConfirmationSent");
        }

        // GET: /User/ConfirmEmail?userId=...&token=...
        [HttpGet]
        public async Task<IActionResult> ConfirmEmail(string userId, string token)
        {
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(token))
                return RedirectToAction("Login");

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return RedirectToAction("Login");

            var result = await _userManager.ConfirmEmailAsync(user, token);

            if (result.Succeeded)
            {
                await _signInManager.SignInAsync(user, isPersistent: false);
                return View("EmailConfirmed");
            }

            ViewBag.Error = "Link za potvrdu nije validan ili je istekao.";
            return View("Login");
        }

        [HttpGet]
        public IActionResult Register_Owner()
        {
            ViewBag.HideHeader = true;
            return View("RegisterOwner");
        }

        [HttpGet]
        public IActionResult RegisterOwner()
        {
            ViewBag.HideHeader = true;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> RegisterOwner(
            string ime, string prezime, string email,
            string password, string confirmPassword)
        {
            ViewBag.HideHeader = true;

            var nameRegex = new System.Text.RegularExpressions.Regex(@"^[a-zA-ZčćžšđČĆŽŠĐ\s\-]+$");

            if (string.IsNullOrWhiteSpace(ime) || !nameRegex.IsMatch(ime))
            {
                ViewBag.Error = "Ime može sadržavati samo slova, razmak i crticu.";
                return View();
            }

            if (string.IsNullOrWhiteSpace(prezime) || !nameRegex.IsMatch(prezime))
            {
                ViewBag.Error = "Prezime može sadržavati samo slova, razmak i crticu.";
                return View();
            }

            if (password != confirmPassword)
            {
                ViewBag.Error = "Passwordi se ne poklapaju.";
                return View();
            }

            var postoji = await _userManager.FindByEmailAsync(email);
            if (postoji != null)
            {
                ViewBag.Error = "Korisnik već postoji.";
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

            await _userManager.AddToRoleAsync(user, "Owner");

            // Email potvrda i za ownera
            await SendConfirmationEmailAsync(user);

            return View("EmailConfirmationSent");
        }

        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login");
            return View(user);
        }

        [HttpPost]
        public async Task<IActionResult> Profile(
            string firstName, string lastName,
            string currentPassword, string newPassword, string confirmPassword)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login");

            user.FirstName = firstName;
            user.LastName = lastName;

            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                ViewBag.Error = "Greška pri ažuriranju profila.";
                return View(user);
            }

            if (!string.IsNullOrWhiteSpace(newPassword))
            {
                if (newPassword != confirmPassword)
                {
                    ViewBag.Error = "Novi passwordi se ne poklapaju.";
                    return View(user);
                }

                var passwordResult = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);
                if (!passwordResult.Succeeded)
                {
                    ViewBag.Error = string.Join(", ", passwordResult.Errors.Select(e => e.Description));
                    return View(user);
                }
            }

            ViewBag.Success = "Profil uspješno ažuriran.";
            return View(user);
        }

        // Dev helper
        public async Task<IActionResult> TestUsers()
        {
            var users = _userManager.Users.ToList();
            return Json(users);
        }

        // ── Helper ───────────────────────────────────────────────────

        private async Task SendConfirmationEmailAsync(ApplicationUser user)
        {
            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var confirmLink = Url.Action("ConfirmEmail", "User",
                new { userId = user.Id, token },
                Request.Scheme);

            var subject = "ParkirajBa – Potvrdite vašu email adresu";
            var body = $@"
                <div style='font-family: Segoe UI, Arial, sans-serif; max-width: 480px; margin: 0 auto; padding: 32px;'>
                    <div style='text-align: center; margin-bottom: 28px;'>
                        <h1 style='color: #6287f9; font-size: 28px; margin: 0;'>ParkirajBa</h1>
                    </div>
                    <div style='background: white; border-radius: 16px; padding: 32px; box-shadow: 0 4px 20px rgba(0,0,0,0.08);'>
                        <h2 style='margin: 0 0 16px; color: #222;'>Dobrodošli, {user.FirstName}!</h2>
                        <p style='color: #555; line-height: 1.6;'>
                            Hvala što ste se registrovali. Kliknite dugme ispod da potvrdite vašu email adresu i aktivirate nalog.
                        </p>
                        <div style='text-align: center; margin: 28px 0;'>
                            <a href='{confirmLink}'
                               style='background: #6287f9; color: white; padding: 14px 32px; border-radius: 30px;
                                      text-decoration: none; font-weight: 600; font-size: 15px; display: inline-block;'>
                                Potvrdi email adresu
                            </a>
                        </div>
                        <p style='color: #aaa; font-size: 12px; text-align: center;'>
                            Link je validan 24 sata. Ako niste kreirali nalog, ignorišite ovaj email.
                        </p>
                    </div>
                </div>";

            await _emailSender.SendEmailAsync(user.Email, subject, body);
        }
    }
}
