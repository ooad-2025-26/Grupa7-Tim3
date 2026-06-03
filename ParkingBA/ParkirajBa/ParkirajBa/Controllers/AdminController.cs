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

        public AdminController(
            ApplicationDbContext database,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            IParkingRepository parkingRepository)
        {
            _database = database;
            _userManager = userManager;
            _roleManager = roleManager;
            _parkingRepository = parkingRepository;
        }

        // ── DASHBOARD ─────────────────────────────────────────
        public async Task<IActionResult> Dashboard()
        {
            var now = DateTime.Now;

            // Users
            var allUsers = _userManager.Users.ToList();
            var userCount = allUsers.Count;
            var ownerCount = (await _userManager.GetUsersInRoleAsync("Owner")).Count;
            var adminCount = (await _userManager.GetUsersInRoleAsync("Admin")).Count;
            var regularCount = userCount - ownerCount - adminCount;

            // Tickets
            var tickets = await _database.Tickets.Include(t => t.ParkingObject).ToListAsync();
            var activeTickets = tickets.Count(t => t.IssuedAt <= now && (!t.ExpiresAt.HasValue || t.ExpiresAt >= now));
            var expiredTickets = tickets.Count(t => t.ExpiresAt.HasValue && t.ExpiresAt < now);
            var paidTickets = tickets.Count(t => t.IsPaid);
            var totalRevenue = tickets.Where(t => t.IsPaid).Sum(t => t.Price);
            var revenueThisMonth = tickets.Where(t => t.IsPaid && t.IssuedAt.Month == now.Month && t.IssuedAt.Year == now.Year).Sum(t => t.Price);
            var revenueLastMonth = tickets.Where(t => t.IsPaid && t.IssuedAt.Month == now.AddMonths(-1).Month && t.IssuedAt.Year == now.AddMonths(-1).Year).Sum(t => t.Price);

            // Parkings
            var parkings = await _parkingRepository.GetAllAsync();
            var totalParkings = parkings.Count;
            var totalSpots = parkings.Sum(p => p.totalSpots ?? 0);
            var availableSpots = parkings.Sum(p => p.availableSpots);

            // Recent activity
            var recentTickets = tickets.OrderByDescending(t => t.IssuedAt).Take(10).ToList();

            // Revenue by month (last 6)
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

        // ── USER MANAGEMENT ───────────────────────────────────
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

        // POST: Toggle user lock
        [HttpPost]
        public async Task<IActionResult> ToggleLock(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();

            if (user.LockoutEnd.HasValue && user.LockoutEnd > DateTimeOffset.Now)
                await _userManager.SetLockoutEndDateAsync(user, null);
            else
                await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.Now.AddYears(100));

            TempData["Success"] = "User lock status updated.";
            return RedirectToAction("Users");
        }

        // POST: Change user role
        [HttpPost]
        public async Task<IActionResult> ChangeRole(string userId, string newRole)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();

            var currentRoles = await _userManager.GetRolesAsync(user);
            await _userManager.RemoveFromRolesAsync(user, currentRoles);
            await _userManager.AddToRoleAsync(user, newRole);

            TempData["Success"] = $"Role changed to {newRole}.";
            return RedirectToAction("Users");
        }

        // POST: Delete user
        [HttpPost]
        public async Task<IActionResult> DeleteUser(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();

            // Remove their tickets first
            var tickets = _database.Tickets.Where(t => t.ApplicationUserId == userId);
            _database.Tickets.RemoveRange(tickets);
            await _database.SaveChangesAsync();

            await _userManager.DeleteAsync(user);
            TempData["Success"] = "User deleted.";
            return RedirectToAction("Users");
        }

        // ── PARKING MANAGEMENT ────────────────────────────────
        public async Task<IActionResult> Parkings()
        {
            var parkings = await _parkingRepository.GetAllWithOwnerAsync();
            return View(parkings);
        }

        // POST: Delete parking
        [HttpPost]
        public async Task<IActionResult> DeleteParking(int id)
        {
            var parking = await _parkingRepository.GetByIdAsync(id);
            if (parking == null) return NotFound();

            var tickets = _database.Tickets.Where(t => t.ParkingObjectId == id);
            _database.Tickets.RemoveRange(tickets);
            _database.ParkingObject.Remove(parking);
            await _database.SaveChangesAsync();

            TempData["Success"] = "Parking deleted.";
            return RedirectToAction("Parkings");
        }

        // ── RESERVATIONS ──────────────────────────────────────
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

        // POST: Delete reservation
        [HttpPost]
        public async Task<IActionResult> DeleteReservation(int id)
        {
            var ticket = await _database.Tickets.Include(t => t.ParkingObject).FirstOrDefaultAsync(t => t.Id == id);
            if (ticket == null) return NotFound();

            if (ticket.ParkingObject != null && ticket.ParkingObject.availableSpots < ticket.ParkingObject.totalSpots)
                ticket.ParkingObject.availableSpots++;

            _database.Tickets.Remove(ticket);
            await _database.SaveChangesAsync();

            TempData["Success"] = "Reservation deleted.";
            return RedirectToAction("Reservations");
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
