using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ParkirajBa.Data;
using ParkirajBa.Models;
using System.Drawing;
using System.Text;

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

        // /Report/Index
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
            ViewBag.ActiveCount = tickets.Count(t => t.ExpiresAt >= DateTime.Now);
            ViewBag.ExpiredCount = tickets.Count(t => t.ExpiresAt.HasValue && t.ExpiresAt < DateTime.Now);

            return View(tickets);
        }

        // /Report/ExportExcel
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

            // ── Title and user info
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

            ws.Row(3).Height = 6; // empty row

            // ── Header
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

            // ── Information
            int row = 5;
            decimal ukupno = 0;
            foreach (var t in tickets)
            {
                bool even = (row % 2 == 0);
                var rowBg = even ? XLColor.FromHtml("#f4f7ff") : XLColor.White;

                ws.Cell(row, 1).Value = row - 4;
                ws.Cell(row, 2).Value = t.ParkingObject?.name ?? "—";
                ws.Cell(row, 3).Value = t.ParkingObject?.address ?? "—";
                ws.Cell(row, 4).Value = t.IssuedAt.ToString("dd.MM.yyyy HH:mm");
                ws.Cell(row, 5).Value = t.ExpiresAt.HasValue ? t.ExpiresAt.Value.ToString("dd.MM.yyyy HH:mm") : "—";
                ws.Cell(row, 6).Value = t.Price;
                ws.Cell(row, 6).Style.NumberFormat.Format = "0.00";

                for (int c = 1; c <= 6; c++)
                    ws.Cell(row, c).Style.Fill.BackgroundColor = rowBg;

                ukupno += t.Price;
                row++;
            }


            ws.Cell(row, 5).Value = "UKUPNO:";
            ws.Cell(row, 5).Style.Font.Bold = true;
            ws.Cell(row, 5).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

            ws.Cell(row, 6).Value = ukupno;
            ws.Cell(row, 6).Style.Font.Bold = true;
            ws.Cell(row, 6).Style.NumberFormat.Format = "0.00";
            ws.Cell(row, 6).Style.Font.FontColor = XLColor.FromHtml("#6287f9");

            // ── Column formating
            ws.Column(1).Width = 5;
            ws.Column(2).Width = 24;
            ws.Column(3).Width = 30;
            ws.Column(4).Width = 18;
            ws.Column(5).Width = 18;
            ws.Column(6).Width = 14;

            // layout for table
            var tableRange = ws.Range(4, 1, row, 6);
            tableRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            tableRange.Style.Border.InsideBorder = XLBorderStyleValues.Hair;

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            stream.Position = 0;

            string fileName = $"Rezervacije_{user.LastName}_{DateTime.Now:yyyyMMdd}.xlsx";
            return File(stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);
        }

        // /Report/ExportPdf
        // Generates a plain HTML page styled for print → user can Ctrl+P or Save as PDF
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

        // GET: /Report/TicketPdf/{id}
        // Single ticket PDF printout
        public async Task<IActionResult> TicketPdf(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "User");

            var ticket = await _database.Tickets
                .Include(t => t.ParkingObject)
                .FirstOrDefaultAsync(t => t.Id == id && t.ApplicationUserId == user.Id);

            if (ticket == null)
            {
                TempData["Error"] = "Tiketa nije pronađena.";
                return RedirectToAction("Index", "Home");
            }

            ViewBag.UserFullName = user.FullName;
            ViewBag.UserEmail = user.Email;

            return View(ticket);
        }
    }
}
