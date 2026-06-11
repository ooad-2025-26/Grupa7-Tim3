using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ParkirajBa.Data;
using ParkirajBa.Models;
using ParkirajBa.Repositories;

namespace ParkirajBa.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _database;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IParkingRepository _parkingRepository;
        private readonly IRequestRepository _requestRepository;

        public AdminController(
            ApplicationDbContext database,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            IRequestRepository requestRepository,
            IParkingRepository parkingRepository)
        {
            _database = database;
            _userManager = userManager;
            _roleManager = roleManager;
            _requestRepository = requestRepository;
            _parkingRepository = parkingRepository;
        }

        public async Task<IActionResult> Dashboard()
        {
            var now = DateTime.Now;

            var allUsers = _userManager.Users.ToList();
            var userCount = allUsers.Count;
            var ownerCount = (await _userManager.GetUsersInRoleAsync("Owner")).Count;
            var adminCount = (await _userManager.GetUsersInRoleAsync("Admin")).Count;
            var regularCount = userCount - ownerCount - adminCount;

            var tickets = await _database.Tickets.Include(t => t.ParkingObject).ToListAsync();
            var activeTickets = tickets.Count(t => t.IssuedAt <= now && (!t.ExpiresAt.HasValue || t.ExpiresAt >= now));
            var expiredTickets = tickets.Count(t => t.ExpiresAt.HasValue && t.ExpiresAt < now);
            var paidTickets = tickets.Count(t => t.IsPaid);
            var totalRevenue = tickets.Where(t => t.IsPaid).Sum(t => t.Price);
            var revenueThisMonth = tickets.Where(t => t.IsPaid && t.IssuedAt.Month == now.Month && t.IssuedAt.Year == now.Year).Sum(t => t.Price);
            var revenueLastMonth = tickets.Where(t => t.IsPaid && t.IssuedAt.Month == now.AddMonths(-1).Month && t.IssuedAt.Year == now.AddMonths(-1).Year).Sum(t => t.Price);

            var parkings = await _parkingRepository.GetAllAsync();
            var totalParkings = parkings.Count;
            var totalSpots = parkings.Sum(p => p.totalSpots ?? 0);
            var availableSpots = parkings.Sum(p => p.availableSpots);

            var recentTickets = tickets.OrderByDescending(t => t.IssuedAt).Take(10).ToList();

            var revenueByMonth = Enumerable.Range(0, 6)
                .Select(i => now.AddMonths(-i))
                .Select(m => new
                {
                    Label = m.ToString("MMM yyyy"),
                    Revenue = tickets.Where(t => t.IsPaid && t.IssuedAt.Month == m.Month && t.IssuedAt.Year == m.Year).Sum(t => t.Price)
                })
                .Reverse()
                .ToList();

            ViewBag.UserCount = userCount;
            ViewBag.OwnerCount = ownerCount;
            ViewBag.AdminCount = adminCount;
            ViewBag.RegularCount = regularCount;
            ViewBag.TotalTickets = tickets.Count;
            ViewBag.ActiveTickets = activeTickets;
            ViewBag.ExpiredTickets = expiredTickets;
            ViewBag.PaidTickets = paidTickets;
            ViewBag.TotalRevenue = totalRevenue;
            ViewBag.RevenueThisMonth = revenueThisMonth;
            ViewBag.RevenueLastMonth = revenueLastMonth;
            ViewBag.TotalParkings = totalParkings;
            ViewBag.TotalSpots = totalSpots;
            ViewBag.AvailableSpots = availableSpots;
            ViewBag.RecentTickets = recentTickets;
            ViewBag.RevenueByMonth = revenueByMonth;

            return View();
        }

        public async Task<IActionResult> Users(string? search, string? role)
        {
            var allUsers = _userManager.Users.ToList();
            var result = new List<AdminUserViewModel>();

            foreach (var u in allUsers)
            {
                var roles = await _userManager.GetRolesAsync(u);
                if (!string.IsNullOrEmpty(role) && !roles.Contains(role)) continue;
                if (!string.IsNullOrEmpty(search) &&
                    !u.FullName.Contains(search, StringComparison.OrdinalIgnoreCase) &&
                    !(u.Email ?? "").Contains(search, StringComparison.OrdinalIgnoreCase)) continue;

                result.Add(new AdminUserViewModel
                {
                    Id = u.Id,
                    FullName = u.FullName ?? "",
                    Email = u.Email ?? "",
                    Roles = roles.ToList(),
                    EmailConfirmed = u.EmailConfirmed,
                    LockoutEnabled = u.LockoutEnabled,
                    LockoutEnd = u.LockoutEnd
                });
            }

            ViewBag.SearchTerm = search;
            ViewBag.RoleFilter = role;
            return View(result);
        }

        [HttpPost]
        public async Task<IActionResult> ToggleLock(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();

            if (user.LockoutEnd.HasValue && user.LockoutEnd > DateTimeOffset.Now)
            {
                await _userManager.SetLockoutEndDateAsync(user, null);
                TempData["Success"] = "Korisnički račun je uspješno otključan.";
            }
            else
            {
                await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.Now.AddYears(100));
                TempData["Success"] = "Korisnički račun je uspješno zaključan.";
            }

            return RedirectToAction("Users");
        }

        [HttpPost]
        public async Task<IActionResult> ChangeRole(string userId, string newRole)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();

            var currentRoles = await _userManager.GetRolesAsync(user);
            await _userManager.RemoveFromRolesAsync(user, currentRoles);

            string prikazUloge = newRole == "Owner" ? "Vlasnik" : (newRole == "Admin" ? "Administrator" : "Korisnik");

            await _userManager.AddToRoleAsync(user, newRole);

            TempData["Success"] = $"Uloga korisnika uspješno promijenjena u {prikazUloge}.";
            return RedirectToAction("Users");
        }

        [HttpPost]
        public async Task<IActionResult> DeleteUser(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();

            var tickets = _database.Tickets.Where(t => t.ApplicationUserId == userId);
            _database.Tickets.RemoveRange(tickets);
            await _database.SaveChangesAsync();

            await _userManager.DeleteAsync(user);
            TempData["Success"] = "Korisnik je uspješno obrisan iz sistema.";
            return RedirectToAction("Users");
        }

        public async Task<IActionResult> Reports(int? year, int? month)
        {
            var now = DateTime.Now;
            int selYear = year ?? now.Year;
            int selMonth = month ?? now.Month;

            var allTickets = await _database.Tickets
                .Include(t => t.ParkingObject)
                .Include(t => t.ApplicationUser)
                .ToListAsync();

            var filtered = allTickets.Where(t => t.IssuedAt.Year == selYear && t.IssuedAt.Month == selMonth).ToList();

            var revenuePerParking = filtered
                .Where(t => t.IsPaid)
                .GroupBy(t => t.ParkingObject?.name ?? "Nepoznato")
                .Select(g => new { Name = g.Key, Revenue = g.Sum(t => t.Price), Count = g.Count() })
                .OrderByDescending(x => x.Revenue)
                .ToList();

            var dailyRevenue = filtered
                .Where(t => t.IsPaid)
                .GroupBy(t => t.IssuedAt.Day)
                .Select(g => new { Day = g.Key, Revenue = g.Sum(t => t.Price) })
                .OrderBy(x => x.Day)
                .ToList();

            ViewBag.SelYear = selYear;
            ViewBag.SelMonth = selMonth;
            ViewBag.TotalRevenue = filtered.Where(t => t.IsPaid).Sum(t => t.Price);
            ViewBag.TotalRes = filtered.Count;
            ViewBag.PaidRes = filtered.Count(t => t.IsPaid);
            ViewBag.UnpaidRes = filtered.Count(t => !t.IsPaid);
            ViewBag.RevenuePerParking = revenuePerParking;
            ViewBag.DailyRevenue = dailyRevenue;
            ViewBag.Years = Enumerable.Range(now.Year - 3, 4).Reverse().ToList();
            return View();
        }

        public async Task<IActionResult> Parkings()
        {
            var parkings = await _parkingRepository.GetAllWithOwnerAsync();
            return View(parkings);
        }
        public async Task<IActionResult> ParkingDetails(int id)
        {
            var parking = await _parkingRepository.GetByIdWithPricingsAsync(id);

            if (parking == null)
            {
                TempData["Error"] = "Parking nije pronađen.";
                return RedirectToAction("Parkings");
            }

            ViewBag.Pricings = await _parkingRepository.GetPricingsByParkingIdAsync(id);
            ViewBag.PrimaryImage = await _parkingRepository.GetPrimaryImageByParkingIDAsync(id);  
            return View(parking);
        }

        [HttpPost]
        public async Task<IActionResult> UploadParkingImage(int id, IFormFile Images)
        {
            if (Images != null && Images.Length > 0)
            {
                var postojeceSlike = await _database.ParkingImages
                    .Where(i => i.ParkingObjectID == id && i.Position == 1)
                    .ToListAsync();
                _database.ParkingImages.RemoveRange(postojeceSlike);
                await _database.SaveChangesAsync();

                await _parkingRepository.SaveParkingImageByIDAsync(Images, 1, id);

                TempData["Success"] = "Slika je uspješno ažurirana.";
            }
            else
            {
                TempData["Error"] = "Molimo odaberite sliku za upload.";
            }

            return RedirectToAction("ParkingDetails", new { id });
        }
        [HttpPost]
        public async Task<IActionResult> DeleteParking(int id)
        {
            var parking = await _parkingRepository.GetByIdAsync(id);
            if (parking == null) return NotFound();

            var tickets = _database.Tickets.Where(t => t.ParkingObjectId == id);
            _database.Tickets.RemoveRange(tickets);
            _database.ParkingObject.Remove(parking);
            await _database.SaveChangesAsync();

            TempData["Success"] = "Parking objekat je uspješno uklonjen iz sistema.";
            return RedirectToAction("Parkings");
        }

        public async Task<IActionResult> Reservations(string? status)
        {
            var now = DateTime.Now;
            var query = _database.Tickets
                .Include(t => t.ParkingObject)
                .Include(t => t.ApplicationUser)
                .AsQueryable();

            if (status == "active")
                query = query.Where(t => t.IssuedAt <= now && (!t.ExpiresAt.HasValue || t.ExpiresAt >= now));
            else if (status == "expired")
                query = query.Where(t => t.ExpiresAt.HasValue && t.ExpiresAt < now);
            else if (status == "paid")
                query = query.Where(t => t.IsPaid);
            else if (status == "unpaid")
                query = query.Where(t => !t.IsPaid);

            var tickets = await query.OrderByDescending(t => t.IssuedAt).ToListAsync();
            ViewBag.StatusFilter = status;
            return View(tickets);
        }

        public async Task<IActionResult> ReservationDetails(int id)
        {
            var ticket = await _database.Tickets
                .Include(t => t.ParkingObject)
                .Include(t => t.ApplicationUser)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (ticket == null)
            {
                TempData["Error"] = "Rezervacija nije pronađena.";
                return RedirectToAction("Reservations");
            }

            ViewBag.IsAdminView = true; 
            return View("~/Views/Reservation/Details.cshtml", ticket);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveParking(int id)
        {
            var parking = await _database.ParkingObject.FindAsync(id);
            if (parking == null) return NotFound();

            parking.isApproved = true;
            await _database.SaveChangesAsync();

            TempData["Success"] = $"Parking \"{parking.name}\" je uspješno odobren.";
            return RedirectToAction("Parkings");
        }
        [HttpPost]
        public async Task<IActionResult> DeleteReservation(int id)
        {
            var ticket = await _database.Tickets.Include(t => t.ParkingObject).FirstOrDefaultAsync(t => t.Id == id);
            if (ticket == null) return NotFound();

            if (ticket.ParkingObject != null && ticket.ParkingObject.availableSpots < ticket.ParkingObject.totalSpots)
                ticket.ParkingObject.availableSpots++;

            _database.Tickets.Remove(ticket);
            await _database.SaveChangesAsync();

            TempData["Success"] = "Rezervacija je uspješno obrisana, a parking mjesto oslobođeno.";
            return RedirectToAction("Reservations");
        }

        public async Task<IActionResult> Requests()
        {
            var requests = await _requestRepository.GetAllAsync();

            var parkingIds = requests.Select(r => r.ParkingID).ToList();
            var parkings = await _database.ParkingObject
                .Where(p => parkingIds.Contains(p.ID))
                .Include(p => p.Owner)
                .Include(p => p.Pricings)
                .ToListAsync();

            ViewBag.Parkings = parkings.ToDictionary(p => p.ID);
            return View(requests);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveRequest(int requestId)
        {
            var request = await _requestRepository.GetByIdAsync(requestId);
            if (request == null)
            {
                TempData["Error"] = "Zahtjev nije pronađen.";
                return RedirectToAction("Requests");
            }

            var parking = await _database.ParkingObject.FindAsync(request.ParkingID);
            if (parking == null)
            {
                TempData["Error"] = "Parking objekat nije pronađen.";
                return RedirectToAction("Requests");
            }

            // approve parking
            parking.isApproved = true;
            _database.ParkingObject.Update(parking);

            // delete request
            await _requestRepository.DeleteAsync(requestId);

            await _database.SaveChangesAsync();

            TempData["Success"] = $"Parking \"{parking.name}\" je uspješno odobren.";
            return RedirectToAction("Requests");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectRequest(int requestId)
        {
            var request = await _requestRepository.GetByIdAsync(requestId);
            if (request == null)
            {
                TempData["Error"] = "Zahtjev nije pronađen.";
                return RedirectToAction("Requests");
            }

            var parking = await _database.ParkingObject
                .Include(p => p.Pricings)
                .FirstOrDefaultAsync(p => p.ID == request.ParkingID);

            if (parking == null)
            {
                TempData["Error"] = "Parking objekat nije pronađen.";
                return RedirectToAction("Requests");
            }

            string parkingName = parking.name;

            // delete images from the database
            var images = await _database.ParkingImages
                .Where(i => i.ParkingObjectID == parking.ID)
                .ToListAsync();

            foreach (var image in images)
            {
                var fullPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", image.ImagePath.TrimStart('/'));
                if (System.IO.File.Exists(fullPath))
                    System.IO.File.Delete(fullPath);
            }

            _database.ParkingImages.RemoveRange(images);

            // delete pricings 
            _database.Pricing.RemoveRange(parking.Pricings);

            // delete parking
            _database.ParkingObject.Remove(parking);

            // delete request
            await _requestRepository.DeleteAsync(requestId);

            await _database.SaveChangesAsync();

            TempData["Success"] = $"Zahtjev za parking \"{parkingName}\" je odbijen i objekat je obrisan.";
            return RedirectToAction("Requests");
        }

    }

    // ── VIEW MODELS ───────────────────────────────────────────
    public class AdminUserViewModel
    {
        public required string Id { get; set; }
        public required string FullName { get; set; }
        public required string Email { get; set; }
        public List<string> Roles { get; set; } = new();
        public bool EmailConfirmed { get; set; }
        public bool LockoutEnabled { get; set; }
        public DateTimeOffset? LockoutEnd { get; set; }
        public bool IsLocked => LockoutEnd.HasValue && LockoutEnd > DateTimeOffset.Now;
    }
}