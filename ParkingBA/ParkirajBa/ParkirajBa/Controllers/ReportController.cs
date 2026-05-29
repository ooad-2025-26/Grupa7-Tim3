using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ParkirajBa.Data;
using ParkirajBa.Models;

namespace ParkirajBa.Controllers
{
    [Authorize]
    public class ReportController : Controller
    {
        private readonly ApplicationDbContext _database;
        private readonly UserManager<ApplicationUser> _userManager;

        public ReportController(ApplicationDbContext database, UserManager<ApplicationUser> userManager)
        {
            _database = database;
            _userManager = userManager;
        }

        // ══════════════════════════════════════════════════════
        //  KORISNIK — pregled svojih rezervacija
        // ══════════════════════════════════════════════════════

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "User");

            var tickets = await _database.Tickets
                .Include(t => t.ParkingObject)
                .Where(t => t.ApplicationUserId == user.Id)
                .OrderByDescending(t => t.IssuedAt)
                .ToListAsync();

            ViewBag.UserFullName = user.FullName;
            ViewBag.UserEmail = user.Email;
            ViewBag.TotalSpent = tickets.Sum(t => t.Price);
            ViewBag.TotalCount = tickets.Count;
            ViewBag.ActiveCount = tickets.Count(t => t.IssuedAt <= DateTime.Now && (!t.ExpiresAt.HasValue || t.ExpiresAt >= DateTime.Now));
            ViewBag.ExpiredCount = tickets.Count(t => t.ExpiresAt.HasValue && t.ExpiresAt < DateTime.Now);

            return View(tickets);
        }

        public async Task<IActionResult> ExportExcel()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "User");

            var tickets = await _database.Tickets
                .Include(t => t.ParkingObject)
                .Where(t => t.ApplicationUserId == user.Id)
                .OrderByDescending(t => t.IssuedAt)
                .ToListAsync();

            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Rezervacije");

            ws.Cell("A1").Value = "ParkirajBa – Izvještaj rezervacija";
            ws.Range("A1:F1").Merge();
            var titleCell = ws.Cell("A1");
            titleCell.Style.Font.Bold = true;
            titleCell.Style.Font.FontSize = 14;
            titleCell.Style.Font.FontColor = XLColor.FromHtml("#6287f9");
            titleCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            ws.Cell("A2").Value = $"Korisnik: {user.FullName}  |  Email: {user.Email}  |  Generisano: {DateTime.Now:dd.MM.yyyy HH:mm}";
            ws.Range("A2:F2").Merge();
            ws.Cell("A2").Style.Font.FontColor = XLColor.Gray;
            ws.Cell("A2").Style.Font.FontSize = 10;
            ws.Cell("A2").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            ws.Row(3).Height = 6;

            string[] headers = { "#", "Parking", "Adresa", "Kreirana", "Ističe", "Cijena (KM)" };
            for (int i = 0; i < headers.Length; i++)
            {
                var cell = ws.Cell(4, i + 1);
                cell.Value = headers[i];
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#6287f9");
                cell.Style.Font.FontColor = XLColor.White;
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            }

            int row = 5;
            decimal ukupno = 0;
            foreach (var t in tickets)
            {
                var rowBg = (row % 2 == 0) ? XLColor.FromHtml("#f4f7ff") : XLColor.White;
                ws.Cell(row, 1).Value = row - 4;
                ws.Cell(row, 2).Value = t.ParkingObject?.name ?? "—";
                ws.Cell(row, 3).Value = t.ParkingObject?.address ?? "—";
                ws.Cell(row, 4).Value = t.IssuedAt.ToString("dd.MM.yyyy HH:mm");
                ws.Cell(row, 5).Value = t.ExpiresAt.HasValue ? t.ExpiresAt.Value.ToString("dd.MM.yyyy HH:mm") : "—";
                ws.Cell(row, 6).Value = t.Price;
                ws.Cell(row, 6).Style.NumberFormat.Format = "0.00";
                for (int c = 1; c <= 6; c++) ws.Cell(row, c).Style.Fill.BackgroundColor = rowBg;
                ukupno += t.Price;
                row++;
            }

            ws.Cell(row, 5).Value = "UKUPNO:";
            ws.Cell(row, 5).Style.Font.Bold = true;
            ws.Cell(row, 6).Value = ukupno;
            ws.Cell(row, 6).Style.Font.Bold = true;
            ws.Cell(row, 6).Style.NumberFormat.Format = "0.00";
            ws.Cell(row, 6).Style.Font.FontColor = XLColor.FromHtml("#6287f9");

            ws.Column(1).Width = 5; ws.Column(2).Width = 24; ws.Column(3).Width = 30;
            ws.Column(4).Width = 18; ws.Column(5).Width = 18; ws.Column(6).Width = 14;
            ws.Range(4, 1, row, 6).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            ws.Range(4, 1, row, 6).Style.Border.InsideBorder = XLBorderStyleValues.Hair;

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return File(stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"Rezervacije_{user.LastName}_{DateTime.Now:yyyyMMdd}.xlsx");
        }

        public async Task<IActionResult> ExportPdf()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "User");

            var tickets = await _database.Tickets
                .Include(t => t.ParkingObject)
                .Where(t => t.ApplicationUserId == user.Id)
                .OrderByDescending(t => t.IssuedAt)
                .ToListAsync();

            ViewBag.UserFullName = user.FullName;
            ViewBag.UserEmail = user.Email;
            ViewBag.GeneratedAt = DateTime.Now.ToString("dd.MM.yyyy HH:mm");
            ViewBag.TotalSpent = tickets.Sum(t => t.Price);
            return View(tickets);
        }

        public async Task<IActionResult> TicketPdf(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "User");

            var ticket = await _database.Tickets
                .Include(t => t.ParkingObject)
                .FirstOrDefaultAsync(t => t.Id == id && t.ApplicationUserId == user.Id);

            if (ticket == null) return RedirectToAction("Index", "Home");

            ViewBag.UserFullName = user.FullName;
            ViewBag.UserEmail = user.Email;
            return View(ticket);
        }

        // ══════════════════════════════════════════════════════
        //  VLASNIK — profit i iskorištenost njegovih parkinga
        // ══════════════════════════════════════════════════════

        [Authorize(Roles = "Owner")]
        public async Task<IActionResult> OwnerReport()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "User");

            var parkings = await _database.ParkingObject
                .Include(p => p.pricings)
                .Where(p => p.OwnerId == user.Id)
                .ToListAsync();

            var parkingIds = parkings.Select(p => p.ID).ToList();

            var tickets = await _database.Tickets
                .Include(t => t.ParkingObject)
                .Where(t => parkingIds.Contains(t.ParkingObjectId))
                .ToListAsync();

            var now = DateTime.Now;

            // Statistike po parkingu
            var parkingStats = parkings.Select(p =>
            {
                var pTickets = tickets.Where(t => t.ParkingObjectId == p.ID).ToList();
                var active = pTickets.Where(t => t.IssuedAt <= now && (!t.ExpiresAt.HasValue || t.ExpiresAt >= now)).ToList();
                double occ = p.totalSpots > 0 ? (double)active.Count / p.totalSpots * 100 : 0;

                return new ParkingStatViewModel
                {
                    Parking = p,
                    TotalTickets = pTickets.Count,
                    ActiveTickets = active.Count,
                    TotalRevenue = pTickets.Sum(t => t.Price),
                    OccupancyPercent = Math.Min(occ, 100),
                    RevenueThisMonth = pTickets
                        .Where(t => t.IssuedAt.Month == now.Month && t.IssuedAt.Year == now.Year)
                        .Sum(t => t.Price),
                    RevenueLastMonth = pTickets
                        .Where(t => t.IssuedAt.Month == now.AddMonths(-1).Month && t.IssuedAt.Year == now.AddMonths(-1).Year)
                        .Sum(t => t.Price),
                };
            }).ToList();

            ViewBag.OwnerName = user.FullName;
            ViewBag.TotalRevenue = parkingStats.Sum(s => s.TotalRevenue);
            ViewBag.TotalTickets = parkingStats.Sum(s => s.TotalTickets);
            ViewBag.TotalParkings = parkings.Count;
            ViewBag.RevenueThisMonth = parkingStats.Sum(s => s.RevenueThisMonth);
            ViewBag.GeneratedAt = now.ToString("dd.MM.yyyy HH:mm");

            return View(parkingStats);
        }

        [Authorize(Roles = "Owner")]
        public async Task<IActionResult> OwnerReportExcel()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "User");

            var parkings = await _database.ParkingObject
                .Where(p => p.OwnerId == user.Id)
                .ToListAsync();

            var parkingIds = parkings.Select(p => p.ID).ToList();

            var tickets = await _database.Tickets
                .Include(t => t.ParkingObject)
                .Where(t => parkingIds.Contains(t.ParkingObjectId))
                .OrderByDescending(t => t.IssuedAt)
                .ToListAsync();

            var now = DateTime.Now;

            using var workbook = new XLWorkbook();

            // ── Sheet 1: Pregled po parkingu ─────────────────────────
            var ws1 = workbook.Worksheets.Add("Pregled parkinga");

            ws1.Cell("A1").Value = "ParkirajBa – Izvještaj vlasnika";
            ws1.Range("A1:G1").Merge();
            ws1.Cell("A1").Style.Font.Bold = true;
            ws1.Cell("A1").Style.Font.FontSize = 14;
            ws1.Cell("A1").Style.Font.FontColor = XLColor.FromHtml("#6287f9");
            ws1.Cell("A1").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            ws1.Cell("A2").Value = $"Vlasnik: {user.FullName}  |  Generisano: {now:dd.MM.yyyy HH:mm}";
            ws1.Range("A2:G2").Merge();
            ws1.Cell("A2").Style.Font.FontColor = XLColor.Gray;
            ws1.Cell("A2").Style.Font.FontSize = 10;
            ws1.Cell("A2").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            ws1.Row(3).Height = 6;

            string[] h1 = { "Parking", "Adresa", "Ukupno mjesta", "Aktivnih rez.", "Iskorištenost", "Prihod ovaj mj.", "Ukupni prihod" };
            for (int i = 0; i < h1.Length; i++)
            {
                var cell = ws1.Cell(4, i + 1);
                cell.Value = h1[i];
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#6287f9");
                cell.Style.Font.FontColor = XLColor.White;
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            }

            int row = 5;
            foreach (var p in parkings)
            {
                var pt = tickets.Where(t => t.ParkingObjectId == p.ID).ToList();
                var active = pt.Where(t => t.IssuedAt <= now && (!t.ExpiresAt.HasValue || t.ExpiresAt >= now)).Count();
                double occ = p.totalSpots > 0 ? Math.Min((double)active / p.totalSpots * 100, 100) : 0;
                var thisMonth = pt.Where(t => t.IssuedAt.Month == now.Month && t.IssuedAt.Year == now.Year).Sum(t => t.Price);

                var bg = (row % 2 == 0) ? XLColor.FromHtml("#f4f7ff") : XLColor.White;
                ws1.Cell(row, 1).Value = p.name ?? "—";
                ws1.Cell(row, 2).Value = p.address ?? "—";
                ws1.Cell(row, 3).Value = p.totalSpots;
                ws1.Cell(row, 4).Value = active;
                ws1.Cell(row, 5).Value = $"{occ:0.0}%";
                ws1.Cell(row, 6).Value = thisMonth; ws1.Cell(row, 6).Style.NumberFormat.Format = "0.00";
                ws1.Cell(row, 7).Value = pt.Sum(t => t.Price); ws1.Cell(row, 7).Style.NumberFormat.Format = "0.00";
                for (int c = 1; c <= 7; c++) ws1.Cell(row, c).Style.Fill.BackgroundColor = bg;
                row++;
            }

            // Ukupno
            ws1.Cell(row, 6).Value = tickets.Where(t => t.IssuedAt.Month == now.Month && t.IssuedAt.Year == now.Year).Sum(t => t.Price);
            ws1.Cell(row, 7).Value = tickets.Sum(t => t.Price);
            ws1.Cell(row, 5).Value = "UKUPNO:";
            for (int c = 5; c <= 7; c++) { ws1.Cell(row, c).Style.Font.Bold = true; ws1.Cell(row, c).Style.NumberFormat.Format = "0.00"; }
            ws1.Cell(row, 7).Style.Font.FontColor = XLColor.FromHtml("#6287f9");

            int[] w1 = { 26, 30, 14, 13, 14, 16, 16 };
            for (int i = 0; i < w1.Length; i++) ws1.Column(i + 1).Width = w1[i];
            ws1.Range(4, 1, row, 7).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            ws1.Range(4, 1, row, 7).Style.Border.InsideBorder = XLBorderStyleValues.Hair;

            // ── Sheet 2: Sve tikete ──────────────────────────────────
            var ws2 = workbook.Worksheets.Add("Sve tikete");

            string[] h2 = { "#", "Parking", "Datum", "Ističe", "Cijena (KM)" };
            for (int i = 0; i < h2.Length; i++)
            {
                var cell = ws2.Cell(1, i + 1);
                cell.Value = h2[i];
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#6287f9");
                cell.Style.Font.FontColor = XLColor.White;
            }

            int r2 = 2;
            foreach (var t in tickets)
            {
                var bg = (r2 % 2 == 0) ? XLColor.FromHtml("#f4f7ff") : XLColor.White;
                ws2.Cell(r2, 1).Value = r2 - 1;
                ws2.Cell(r2, 2).Value = t.ParkingObject?.name ?? "—";
                ws2.Cell(r2, 3).Value = t.IssuedAt.ToString("dd.MM.yyyy HH:mm");
                ws2.Cell(r2, 4).Value = t.ExpiresAt.HasValue ? t.ExpiresAt.Value.ToString("dd.MM.yyyy HH:mm") : "—";
                ws2.Cell(r2, 5).Value = t.Price; ws2.Cell(r2, 5).Style.NumberFormat.Format = "0.00";
                for (int c = 1; c <= 5; c++) ws2.Cell(r2, c).Style.Fill.BackgroundColor = bg;
                r2++;
            }
            ws2.Column(1).Width = 5; ws2.Column(2).Width = 26; ws2.Column(3).Width = 18;
            ws2.Column(4).Width = 18; ws2.Column(5).Width = 14;
            ws2.Range(1, 1, r2 - 1, 5).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            ws2.Range(1, 1, r2 - 1, 5).Style.Border.InsideBorder = XLBorderStyleValues.Hair;

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return File(stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"Izvjestaj_Vlasnik_{DateTime.Now:yyyyMMdd}.xlsx");
        }

        [Authorize(Roles = "Owner")]
        public async Task<IActionResult> OwnerReportPdf()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "User");

            var parkings = await _database.ParkingObject
                .Where(p => p.OwnerId == user.Id)
                .ToListAsync();

            var parkingIds = parkings.Select(p => p.ID).ToList();
            var tickets = await _database.Tickets
                .Include(t => t.ParkingObject)
                .Where(t => parkingIds.Contains(t.ParkingObjectId))
                .ToListAsync();

            var now = DateTime.Now;
            var stats = parkings.Select(p =>
            {
                var pt = tickets.Where(t => t.ParkingObjectId == p.ID).ToList();
                var active = pt.Where(t => t.IssuedAt <= now && (!t.ExpiresAt.HasValue || t.ExpiresAt >= now)).Count();
                return new ParkingStatViewModel
                {
                    Parking = p,
                    TotalTickets = pt.Count,
                    ActiveTickets = active,
                    TotalRevenue = pt.Sum(t => t.Price),
                    OccupancyPercent = p.totalSpots > 0 ? Math.Min((double)active / p.totalSpots * 100, 100) : 0,
                    RevenueThisMonth = pt.Where(t => t.IssuedAt.Month == now.Month && t.IssuedAt.Year == now.Year).Sum(t => t.Price),
                    RevenueLastMonth = pt.Where(t => t.IssuedAt.Month == now.AddMonths(-1).Month && t.IssuedAt.Year == now.AddMonths(-1).Year).Sum(t => t.Price),
                };
            }).ToList();

            ViewBag.OwnerName = user.FullName;
            ViewBag.TotalRevenue = stats.Sum(s => s.TotalRevenue);
            ViewBag.RevenueThisMonth = stats.Sum(s => s.RevenueThisMonth);
            ViewBag.GeneratedAt = now.ToString("dd.MM.yyyy HH:mm");
            return View(stats);
        }

        // ══════════════════════════════════════════════════════
        //  ADMIN — pregled svih parkinga u sistemu
        // ══════════════════════════════════════════════════════

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AdminReport()
        {
            var now = DateTime.Now;

            var parkings = await _database.ParkingObject
                .Include(p => p.Owner)
                .Include(p => p.pricings)
                .ToListAsync();

            var tickets = await _database.Tickets
                .Include(t => t.ParkingObject)
                .ToListAsync();

            var stats = parkings.Select(p =>
            {
                var pt = tickets.Where(t => t.ParkingObjectId == p.ID).ToList();
                var active = pt.Where(t => t.IssuedAt <= now && (!t.ExpiresAt.HasValue || t.ExpiresAt >= now)).Count();
                return new ParkingStatViewModel
                {
                    Parking = p,
                    TotalTickets = pt.Count,
                    ActiveTickets = active,
                    TotalRevenue = pt.Sum(t => t.Price),
                    OccupancyPercent = p.totalSpots > 0 ? Math.Min((double)active / p.totalSpots * 100, 100) : 0,
                    RevenueThisMonth = pt.Where(t => t.IssuedAt.Month == now.Month && t.IssuedAt.Year == now.Year).Sum(t => t.Price),
                    RevenueLastMonth = pt.Where(t => t.IssuedAt.Month == now.AddMonths(-1).Month && t.IssuedAt.Year == now.AddMonths(-1).Year).Sum(t => t.Price),
                };
            }).OrderByDescending(s => s.TotalRevenue).ToList();

            ViewBag.TotalRevenue = stats.Sum(s => s.TotalRevenue);
            ViewBag.TotalTickets = tickets.Count;
            ViewBag.TotalParkings = parkings.Count;
            ViewBag.RevenueThisMonth = stats.Sum(s => s.RevenueThisMonth);
            ViewBag.TotalUsers = await _database.Users.CountAsync();
            ViewBag.GeneratedAt = now.ToString("dd.MM.yyyy HH:mm");

            return View(stats);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AdminReportExcel()
        {
            var now = DateTime.Now;

            var parkings = await _database.ParkingObject
                .Include(p => p.Owner)
                .ToListAsync();

            var tickets = await _database.Tickets
                .Include(t => t.ParkingObject)
                .OrderByDescending(t => t.IssuedAt)
                .ToListAsync();

            using var workbook = new XLWorkbook();

            // ── Sheet 1: Pregled svih parkinga ──────────────────────
            var ws1 = workbook.Worksheets.Add("Svi parkings");

            ws1.Cell("A1").Value = "ParkirajBa – Admin izvještaj";
            ws1.Range("A1:H1").Merge();
            ws1.Cell("A1").Style.Font.Bold = true;
            ws1.Cell("A1").Style.Font.FontSize = 14;
            ws1.Cell("A1").Style.Font.FontColor = XLColor.FromHtml("#6287f9");
            ws1.Cell("A1").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            ws1.Cell("A2").Value = $"Generisano: {now:dd.MM.yyyy HH:mm}  |  Ukupno parkinga: {parkings.Count}  |  Ukupno tiketa: {tickets.Count}";
            ws1.Range("A2:H2").Merge();
            ws1.Cell("A2").Style.Font.FontColor = XLColor.Gray;
            ws1.Cell("A2").Style.Font.FontSize = 10;
            ws1.Cell("A2").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            ws1.Row(3).Height = 6;

            string[] h1 = { "Parking", "Adresa", "Vlasnik", "Ukupno mjesta", "Aktivnih rez.", "Iskorištenost", "Prihod ovaj mj.", "Ukupni prihod" };
            for (int i = 0; i < h1.Length; i++)
            {
                var cell = ws1.Cell(4, i + 1);
                cell.Value = h1[i];
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#6287f9");
                cell.Style.Font.FontColor = XLColor.White;
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            }

            int row = 5;
            foreach (var p in parkings.OrderByDescending(p => tickets.Where(t => t.ParkingObjectId == p.ID).Sum(t => t.Price)))
            {
                var pt = tickets.Where(t => t.ParkingObjectId == p.ID).ToList();
                var active = pt.Where(t => t.IssuedAt <= now && (!t.ExpiresAt.HasValue || t.ExpiresAt >= now)).Count();
                double occ = p.totalSpots > 0 ? Math.Min((double)active / p.totalSpots * 100, 100) : 0;
                var thisMonth = pt.Where(t => t.IssuedAt.Month == now.Month && t.IssuedAt.Year == now.Year).Sum(t => t.Price);

                var bg = (row % 2 == 0) ? XLColor.FromHtml("#f4f7ff") : XLColor.White;
                ws1.Cell(row, 1).Value = p.name ?? "—";
                ws1.Cell(row, 2).Value = p.address ?? "—";
                ws1.Cell(row, 3).Value = p.Owner?.FullName ?? "—";
                ws1.Cell(row, 4).Value = p.totalSpots;
                ws1.Cell(row, 5).Value = active;
                ws1.Cell(row, 6).Value = $"{occ:0.0}%";
                ws1.Cell(row, 7).Value = thisMonth; ws1.Cell(row, 7).Style.NumberFormat.Format = "0.00";
                ws1.Cell(row, 8).Value = pt.Sum(t => t.Price); ws1.Cell(row, 8).Style.NumberFormat.Format = "0.00";
                for (int c = 1; c <= 8; c++) ws1.Cell(row, c).Style.Fill.BackgroundColor = bg;
                row++;
            }

            ws1.Cell(row, 7).Value = tickets.Where(t => t.IssuedAt.Month == now.Month && t.IssuedAt.Year == now.Year).Sum(t => t.Price);
            ws1.Cell(row, 8).Value = tickets.Sum(t => t.Price);
            ws1.Cell(row, 6).Value = "UKUPNO:";
            for (int c = 6; c <= 8; c++) { ws1.Cell(row, c).Style.Font.Bold = true; ws1.Cell(row, c).Style.NumberFormat.Format = "0.00"; }
            ws1.Cell(row, 8).Style.Font.FontColor = XLColor.FromHtml("#6287f9");

            int[] w1 = { 26, 30, 22, 14, 13, 14, 16, 16 };
            for (int i = 0; i < w1.Length; i++) ws1.Column(i + 1).Width = w1[i];
            ws1.Range(4, 1, row, 8).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            ws1.Range(4, 1, row, 8).Style.Border.InsideBorder = XLBorderStyleValues.Hair;

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return File(stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"Admin_Izvjestaj_{now:yyyyMMdd}.xlsx");
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AdminReportPdf()
        {
            var now = DateTime.Now;

            var parkings = await _database.ParkingObject
                .Include(p => p.Owner)
                .ToListAsync();

            var tickets = await _database.Tickets
                .Include(t => t.ParkingObject)
                .ToListAsync();

            var stats = parkings.Select(p =>
            {
                var pt = tickets.Where(t => t.ParkingObjectId == p.ID).ToList();
                var active = pt.Where(t => t.IssuedAt <= now && (!t.ExpiresAt.HasValue || t.ExpiresAt >= now)).Count();
                return new ParkingStatViewModel
                {
                    Parking = p,
                    TotalTickets = pt.Count,
                    ActiveTickets = active,
                    TotalRevenue = pt.Sum(t => t.Price),
                    OccupancyPercent = p.totalSpots > 0 ? Math.Min((double)active / p.totalSpots * 100, 100) : 0,
                    RevenueThisMonth = pt.Where(t => t.IssuedAt.Month == now.Month && t.IssuedAt.Year == now.Year).Sum(t => t.Price),
                    RevenueLastMonth = pt.Where(t => t.IssuedAt.Month == now.AddMonths(-1).Month && t.IssuedAt.Year == now.AddMonths(-1).Year).Sum(t => t.Price),
                };
            }).OrderByDescending(s => s.TotalRevenue).ToList();

            ViewBag.TotalRevenue = stats.Sum(s => s.TotalRevenue);
            ViewBag.TotalTickets = tickets.Count;
            ViewBag.TotalParkings = parkings.Count;
            ViewBag.TotalUsers = await _database.Users.CountAsync();
            ViewBag.RevenueThisMonth = stats.Sum(s => s.RevenueThisMonth);
            ViewBag.GeneratedAt = now.ToString("dd.MM.yyyy HH:mm");
            return View(stats);
        }
    }

    // ── ViewModel ─────────────────────────────────────────────
    public class ParkingStatViewModel
    {
        public ParkingObject Parking { get; set; }
        public int TotalTickets { get; set; }
        public int ActiveTickets { get; set; }
        public decimal TotalRevenue { get; set; }
        public double OccupancyPercent { get; set; }
        public decimal RevenueThisMonth { get; set; }
        public decimal RevenueLastMonth { get; set; }
    }
}
