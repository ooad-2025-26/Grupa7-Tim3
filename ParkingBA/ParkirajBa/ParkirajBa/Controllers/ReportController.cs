using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ParkirajBa.Data;
using ParkirajBa.Models;
using ParkirajBa.Repositories;

namespace ParkirajBa.Controllers
{
    [Authorize]
    public class ReportController : Controller
    {
        private readonly ApplicationDbContext _database;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IParkingRepository _parkingRepository;

        public ReportController(
            ApplicationDbContext database,
            UserManager<ApplicationUser> userManager,
            IParkingRepository parkingRepository)
        {
            _database = database;
            _userManager = userManager;
            _parkingRepository = parkingRepository;
        }

        // ══════════════════════════════════════════════════════
        //  KORISNIK
        // ══════════════════════════════════════════════════════

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "User");

            var tickets = await _parkingRepository.GetTicketsByUserIdAsync(user.Id);

            var now = DateTime.Now;
            ViewBag.UserFullName = user.FullName;
            ViewBag.UserEmail = user.Email;
            ViewBag.TotalSpent = tickets.Sum(t => t.Price);
            ViewBag.TotalCount = tickets.Count;
            ViewBag.ActiveCount = tickets.Count(t => t.IssuedAt <= now && (!t.ExpiresAt.HasValue || t.ExpiresAt >= now));
            ViewBag.ExpiredCount = tickets.Count(t => t.ExpiresAt.HasValue && t.ExpiresAt < now);

            return View(tickets);
        }

        public async Task<IActionResult> ExportExcel()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "User");

            var tickets = await _parkingRepository.GetTicketsByUserIdAsync(user.Id);

            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Rezervacije");

            BuildExcelHeader(ws, $"ParkirajBa – Izvještaj rezervacija",
                $"Korisnik: {user.FullName}  |  Email: {user.Email}  |  Generisano: {DateTime.Now:dd.MM.yyyy HH:mm}", 6);

            string[] headers = { "#", "Parking", "Adresa", "Kreirana", "Ističe", "Cijena (KM)" };
            WriteExcelHeaders(ws, 4, headers);

            int row = 5;
            decimal ukupno = 0;
            foreach (var t in tickets)
            {
                var bg = (row % 2 == 0) ? XLColor.FromHtml("#f4f7ff") : XLColor.White;
                ws.Cell(row, 1).Value = row - 4;
                ws.Cell(row, 2).Value = t.ParkingObject?.name ?? "—";
                ws.Cell(row, 3).Value = t.ParkingObject?.address ?? "—";
                ws.Cell(row, 4).Value = t.IssuedAt.ToString("dd.MM.yyyy HH:mm");
                ws.Cell(row, 5).Value = t.ExpiresAt.HasValue ? t.ExpiresAt.Value.ToString("dd.MM.yyyy HH:mm") : "—";
                ws.Cell(row, 6).Value = (double)t.Price;
                ws.Cell(row, 6).Style.NumberFormat.Format = "0.00";
                for (int c = 1; c <= 6; c++) ws.Cell(row, c).Style.Fill.BackgroundColor = bg;
                ukupno += t.Price;
                row++;
            }

            WriteExcelTotal(ws, row, 5, (double)ukupno, 6);
            SetColumnWidths(ws, new[] { 5, 24, 30, 18, 18, 14 });
            StyleExcelTable(ws, 4, row, 6);

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

            var tickets = await _parkingRepository.GetTicketsByUserIdAsync(user.Id);

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

            var ticket = await _parkingRepository.GetTicketByIdAsync(id, user.Id);

            if (ticket == null) return RedirectToAction("Index", "Home");

            ViewBag.UserFullName = user.FullName;
            ViewBag.UserEmail = user.Email;
            return View(ticket);
        }

        // ══════════════════════════════════════════════════════
        //  VLASNIK
        // ══════════════════════════════════════════════════════

        [Authorize(Roles = "Owner")]
        public async Task<IActionResult> OwnerReport()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "User");

            var parkings = await _parkingRepository.GetByOwnerIdAsync(user.Id);
            var stats = await BuildParkingStatsAsync(parkings);

            ViewBag.OwnerName = user.FullName;
            ViewBag.TotalRevenue = stats.Sum(s => s.TotalRevenue);
            ViewBag.TotalTickets = stats.Sum(s => s.TotalTickets);
            ViewBag.TotalParkings = parkings.Count;
            ViewBag.RevenueThisMonth = stats.Sum(s => s.RevenueThisMonth);
            ViewBag.GeneratedAt = DateTime.Now.ToString("dd.MM.yyyy HH:mm");

            return View(stats);
        }

        [Authorize(Roles = "Owner")]
        public async Task<IActionResult> OwnerReportExcel()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "User");

            var parkings = await _parkingRepository.GetByOwnerIdAsync(user.Id);
            var tickets = await _parkingRepository.GetTicketsByParkingIdsAsync(parkings.Select(p => p.ID).ToList());
            var now = DateTime.Now;

            using var workbook = new XLWorkbook();

            // Sheet 1 — pregled po parkingu
            var ws1 = workbook.Worksheets.Add("Pregled parkinga");
            BuildExcelHeader(ws1, "ParkirajBa – Izvještaj vlasnika",
                $"Vlasnik: {user.FullName}  |  Generisano: {now:dd.MM.yyyy HH:mm}", 7);

            string[] h1 = { "Parking", "Adresa", "Ukupno mjesta", "Aktivnih rez.", "Iskorištenost", "Prihod ovaj mj.", "Ukupni prihod" };
            WriteExcelHeaders(ws1, 4, h1);

            int row = 5;
            foreach (var p in parkings)
            {
                var pt = tickets.Where(t => t.ParkingObjectId == p.ID).ToList();
                var active = pt.Count(t => t.IssuedAt <= now && (!t.ExpiresAt.HasValue || t.ExpiresAt >= now));
                double occ = p.totalSpots > 0 ? Math.Min((double)active / (double)p.totalSpots * 100, 100) : 0;
                var thisMonth = pt.Where(t => t.IssuedAt.Month == now.Month && t.IssuedAt.Year == now.Year).Sum(t => t.Price);

                var bg = (row % 2 == 0) ? XLColor.FromHtml("#f4f7ff") : XLColor.White;
                ws1.Cell(row, 1).Value = p.name ?? "—";
                ws1.Cell(row, 2).Value = p.address ?? "—";
                ws1.Cell(row, 3).Value = p.totalSpots;
                ws1.Cell(row, 4).Value = active;
                ws1.Cell(row, 5).Value = $"{occ:0.0}%";
                ws1.Cell(row, 6).Value = (double)thisMonth; ws1.Cell(row, 6).Style.NumberFormat.Format = "0.00";
                ws1.Cell(row, 7).Value = (double)pt.Sum(t => t.Price); ws1.Cell(row, 7).Style.NumberFormat.Format = "0.00";
                for (int c = 1; c <= 7; c++) ws1.Cell(row, c).Style.Fill.BackgroundColor = bg;
                row++;
            }

            WriteExcelTotal(ws1, row, 6,
                (double)tickets.Where(t => t.IssuedAt.Month == now.Month && t.IssuedAt.Year == now.Year).Sum(t => t.Price), 6);
            ws1.Cell(row, 7).Value = (double)tickets.Sum(t => t.Price);
            ws1.Cell(row, 7).Style.Font.Bold = true;
            ws1.Cell(row, 7).Style.NumberFormat.Format = "0.00";
            ws1.Cell(row, 7).Style.Font.FontColor = XLColor.FromHtml("#6287f9");
            SetColumnWidths(ws1, new[] { 26, 30, 14, 13, 14, 16, 16 });
            StyleExcelTable(ws1, 4, row, 7);

            // Sheet 2 — sve tikete
            var ws2 = workbook.Worksheets.Add("Sve tikete");
            WriteExcelHeaders(ws2, 1, new[] { "#", "Parking", "Datum", "Ističe", "Cijena (KM)" });
            int r2 = 2;
            foreach (var t in tickets.OrderByDescending(t => t.IssuedAt))
            {
                var bg = (r2 % 2 == 0) ? XLColor.FromHtml("#f4f7ff") : XLColor.White;
                ws2.Cell(r2, 1).Value = r2 - 1;
                ws2.Cell(r2, 2).Value = t.ParkingObject?.name ?? "—";
                ws2.Cell(r2, 3).Value = t.IssuedAt.ToString("dd.MM.yyyy HH:mm");
                ws2.Cell(r2, 4).Value = t.ExpiresAt.HasValue ? t.ExpiresAt.Value.ToString("dd.MM.yyyy HH:mm") : "—";
                ws2.Cell(r2, 5).Value = (double)t.Price; ws2.Cell(r2, 5).Style.NumberFormat.Format = "0.00";
                for (int c = 1; c <= 5; c++) ws2.Cell(r2, c).Style.Fill.BackgroundColor = bg;
                r2++;
            }
            SetColumnWidths(ws2, new[] { 5, 26, 18, 18, 14 });
            if (r2 > 2) StyleExcelTable(ws2, 1, r2 - 1, 5);

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

            var parkings = await _parkingRepository.GetByOwnerIdAsync(user.Id);
            var stats = await BuildParkingStatsAsync(parkings);

            ViewBag.OwnerName = user.FullName;
            ViewBag.TotalRevenue = stats.Sum(s => s.TotalRevenue);
            ViewBag.RevenueThisMonth = stats.Sum(s => s.RevenueThisMonth);
            ViewBag.GeneratedAt = DateTime.Now.ToString("dd.MM.yyyy HH:mm");
            return View(stats);
        }

        // ══════════════════════════════════════════════════════
        //  ADMIN
        // ══════════════════════════════════════════════════════

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AdminReport()
        {
            var parkings = await _parkingRepository.GetAllWithOwnerAsync();
            var stats = await BuildParkingStatsAsync(parkings);
            var now = DateTime.Now;

            ViewBag.TotalRevenue = stats.Sum(s => s.TotalRevenue);
            ViewBag.TotalTickets = stats.Sum(s => s.TotalTickets);
            ViewBag.TotalParkings = parkings.Count;
            ViewBag.RevenueThisMonth = stats.Sum(s => s.RevenueThisMonth);
            ViewBag.TotalUsers = await _database.Users.CountAsync();
            ViewBag.GeneratedAt = now.ToString("dd.MM.yyyy HH:mm");

            return View(stats.OrderByDescending(s => s.TotalRevenue).ToList());
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AdminReportExcel()
        {
            var now = DateTime.Now;
            var parkings = await _parkingRepository.GetAllWithOwnerAsync();
            var allTickets = await _parkingRepository.GetTicketsByParkingIdsAsync(parkings.Select(p => p.ID).ToList());

            using var workbook = new XLWorkbook();
            var ws1 = workbook.Worksheets.Add("Svi parkings");
            BuildExcelHeader(ws1, "ParkirajBa – Admin izvještaj",
                $"Generisano: {now:dd.MM.yyyy HH:mm}  |  Parkinga: {parkings.Count}  |  Tiketa: {allTickets.Count}", 8);

            string[] h1 = { "Parking", "Adresa", "Vlasnik", "Ukupno mjesta", "Aktivnih rez.", "Iskorištenost", "Prihod ovaj mj.", "Ukupni prihod" };
            WriteExcelHeaders(ws1, 4, h1);

            int row = 5;
            foreach (var p in parkings.OrderByDescending(p => allTickets.Where(t => t.ParkingObjectId == p.ID).Sum(t => t.Price)))
            {
                var pt = allTickets.Where(t => t.ParkingObjectId == p.ID).ToList();
                var active = pt.Count(t => t.IssuedAt <= now && (!t.ExpiresAt.HasValue || t.ExpiresAt >= now));
                double occ = p.totalSpots > 0 ? Math.Min((double)active / (double)p.totalSpots * 100, 100) : 0;
                var thisMonth = pt.Where(t => t.IssuedAt.Month == now.Month && t.IssuedAt.Year == now.Year).Sum(t => t.Price);

                var bg = (row % 2 == 0) ? XLColor.FromHtml("#f4f7ff") : XLColor.White;
                ws1.Cell(row, 1).Value = p.name ?? "—";
                ws1.Cell(row, 2).Value = p.address ?? "—";
                ws1.Cell(row, 3).Value = p.Owner?.FullName ?? "—";
                ws1.Cell(row, 4).Value = p.totalSpots;
                ws1.Cell(row, 5).Value = active;
                ws1.Cell(row, 6).Value = $"{occ:0.0}%";
                ws1.Cell(row, 7).Value = (double)thisMonth; ws1.Cell(row, 7).Style.NumberFormat.Format = "0.00";
                ws1.Cell(row, 8).Value = (double)pt.Sum(t => t.Price); ws1.Cell(row, 8).Style.NumberFormat.Format = "0.00";
                for (int c = 1; c <= 8; c++) ws1.Cell(row, c).Style.Fill.BackgroundColor = bg;
                row++;
            }

            WriteExcelTotal(ws1, row, 7,
                (double)allTickets.Where(t => t.IssuedAt.Month == now.Month && t.IssuedAt.Year == now.Year).Sum(t => t.Price), 7);
            ws1.Cell(row, 8).Value = (double)allTickets.Sum(t => t.Price);
            ws1.Cell(row, 8).Style.Font.Bold = true;
            ws1.Cell(row, 8).Style.NumberFormat.Format = "0.00";
            ws1.Cell(row, 8).Style.Font.FontColor = XLColor.FromHtml("#6287f9");
            SetColumnWidths(ws1, new[] { 26, 30, 22, 14, 13, 14, 16, 16 });
            StyleExcelTable(ws1, 4, row, 8);

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
            var parkings = await _parkingRepository.GetAllWithOwnerAsync();
            var stats = await BuildParkingStatsAsync(parkings);

            ViewBag.TotalRevenue = stats.Sum(s => s.TotalRevenue);
            ViewBag.TotalTickets = stats.Sum(s => s.TotalTickets);
            ViewBag.TotalParkings = parkings.Count;
            ViewBag.TotalUsers = await _database.Users.CountAsync();
            ViewBag.RevenueThisMonth = stats.Sum(s => s.RevenueThisMonth);
            ViewBag.GeneratedAt = now.ToString("dd.MM.yyyy HH:mm");
            return View(stats.OrderByDescending(s => s.TotalRevenue).ToList());
        }

        // ══════════════════════════════════════════════════════
        //  PRIVATE HELPERS
        // ══════════════════════════════════════════════════════

        private async Task<List<ParkingStatViewModel>> BuildParkingStatsAsync(List<ParkingObject> parkings)
        {
            var tickets = await _parkingRepository.GetTicketsByParkingIdsAsync(parkings.Select(p => p.ID).ToList());
            var now = DateTime.Now;

            return parkings.Select(p =>
            {
                var pt = tickets.Where(t => t.ParkingObjectId == p.ID).ToList();
                var active = pt.Count(t => t.IssuedAt <= now && (!t.ExpiresAt.HasValue || t.ExpiresAt >= now));

                return new ParkingStatViewModel
                {
                    Parking = p,
                    TotalTickets = pt.Count,
                    ActiveTickets = active,
                    TotalRevenue = pt.Sum(t => t.Price),
                    OccupancyPercent = p.totalSpots > 0 ? Math.Min((double)active / (double)p.totalSpots * 100, 100) : 0,
                    RevenueThisMonth = pt.Where(t => t.IssuedAt.Month == now.Month && t.IssuedAt.Year == now.Year).Sum(t => t.Price),
                    RevenueLastMonth = pt.Where(t => t.IssuedAt.Month == now.AddMonths(-1).Month && t.IssuedAt.Year == now.AddMonths(-1).Year).Sum(t => t.Price),
                };
            }).ToList();
        }

        private static void BuildExcelHeader(IXLWorksheet ws, string title, string subtitle, int cols)
        {
            ws.Range(1, 1, 1, cols).Merge();
            ws.Cell("A1").Value = title;
            ws.Cell("A1").Style.Font.Bold = true;
            ws.Cell("A1").Style.Font.FontSize = 14;
            ws.Cell("A1").Style.Font.FontColor = XLColor.FromHtml("#6287f9");
            ws.Cell("A1").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            ws.Range(2, 1, 2, cols).Merge();
            ws.Cell("A2").Value = subtitle;
            ws.Cell("A2").Style.Font.FontColor = XLColor.Gray;
            ws.Cell("A2").Style.Font.FontSize = 10;
            ws.Cell("A2").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            ws.Row(3).Height = 6;
        }

        private static void WriteExcelHeaders(IXLWorksheet ws, int row, string[] headers)
        {
            for (int i = 0; i < headers.Length; i++)
            {
                var cell = ws.Cell(row, i + 1);
                cell.Value = headers[i];
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#6287f9");
                cell.Style.Font.FontColor = XLColor.White;
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            }
        }

        private static void WriteExcelTotal(IXLWorksheet ws, int row, int labelCol, double value, int valueCol)
        {
            ws.Cell(row, labelCol).Value = "UKUPNO:";
            ws.Cell(row, labelCol).Style.Font.Bold = true;
            ws.Cell(row, labelCol).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
            ws.Cell(row, valueCol).Value = value;
            ws.Cell(row, valueCol).Style.Font.Bold = true;
            ws.Cell(row, valueCol).Style.NumberFormat.Format = "0.00";
            ws.Cell(row, valueCol).Style.Font.FontColor = XLColor.FromHtml("#6287f9");
        }

        private static void SetColumnWidths(IXLWorksheet ws, int[] widths)
        {
            for (int i = 0; i < widths.Length; i++)
                ws.Column(i + 1).Width = widths[i];
        }

        private static void StyleExcelTable(IXLWorksheet ws, int fromRow, int toRow, int cols)
        {
            var range = ws.Range(fromRow, 1, toRow, cols);
            range.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            range.Style.Border.InsideBorder = XLBorderStyleValues.Hair;
        }
    }

    public class ParkingStatViewModel
    {
        public required ParkingObject Parking { get; set; }
        public int TotalTickets { get; set; }
        public int ActiveTickets { get; set; }
        public decimal TotalRevenue { get; set; }
        public double OccupancyPercent { get; set; }
        public decimal RevenueThisMonth { get; set; }
        public decimal RevenueLastMonth { get; set; }
    }
}