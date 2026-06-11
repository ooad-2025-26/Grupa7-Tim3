using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Net;
using System.Net.Mail;
using QRCoder;
using System.IO;
using ParkirajBa.Models;
using ParkirajBa.Data;
using ParkirajBa.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;

namespace ParkirajBa.Controllers
{
    [Authorize]
    public class PaymentController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _database;
        private readonly IParkingRepository _parkingRepository;
        private readonly string _senderEmail;
        private readonly string _senderPassword;

        public PaymentController(
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext database,
            IParkingRepository parkingRepository,
            IConfiguration configuration)
        {
            _userManager = userManager;
            _database = database;
            _parkingRepository = parkingRepository;
            _senderEmail = configuration["EmailSettings:SenderEmail"]
                ?? throw new InvalidOperationException("EmailSettings:SenderEmail is not configured.");
            _senderPassword = configuration["EmailSettings:AppPassword"]
                ?? throw new InvalidOperationException("EmailSettings:AppPassword is not configured.");
        }

        // GET: /Payment/Checkout?ticketId=5
        [HttpGet]
        public async Task<IActionResult> Checkout(int ticketId, bool additionalCharge = false, bool isExtension = false, int originalTicketId = 0)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "User");

            var ticket = await _parkingRepository.GetTicketByIdAsync(ticketId, user.Id);
            if (ticket == null) return RedirectToAction("Reservations");

            ViewBag.Ticket = ticket;
            ViewBag.UserFullName = user.FullName;
            ViewBag.IsAdditionalCharge = additionalCharge;
            ViewBag.IsExtension = isExtension;
            ViewBag.OriginalTicketId = originalTicketId;
            return View();
        }

        // GET: /Payment/Confirm?cardName=...&ticketId=5
        [HttpGet]
        public async Task<IActionResult> Confirm(string cardName, int ticketId, bool additionalCharge = false, bool isExtension = false, int originalTicketId = 0)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "User");

            var ticket = await _parkingRepository.GetTicketByIdAsync(ticketId, user.Id);
            if (ticket == null) return RedirectToAction("Checkout", new { ticketId });

            ViewBag.CardName = cardName;
            ViewBag.Ticket = ticket;
            ViewBag.IsAdditionalCharge = additionalCharge;
            ViewBag.IsExtension = isExtension;
            ViewBag.OriginalTicketId = originalTicketId;
            return View();
        }

        // POST: /Payment/SendConfirmationEmail
        [HttpPost]
        public async Task<IActionResult> SendConfirmationEmail(string cardName, int ticketId, bool additionalCharge = false, bool isExtension = false, int originalTicketId = 0)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var ticket = await _parkingRepository.GetTicketByIdAsync(ticketId, user.Id);
            if (ticket == null) return RedirectToAction("Checkout", new { ticketId });

            //Damir changes
            string reservationCode = "PB-" + Guid.NewGuid().ToString().Substring(0, 8).ToUpper();

            // Mark ticket as paid + save QR data
            bool isAdditionalCharge = ticket.AdditionalCharge > 0 && !ticket.AdditionalChargePaid;

            decimal paidAmount = ticket.Price;

            if (isExtension && originalTicketId > 0)
            {
                // Produženje rezervacije: ažurirati ExpiresAt originalnog ticketa
                var originalTicket = await _database.Tickets
                    .FirstOrDefaultAsync(t => t.Id == originalTicketId && t.ApplicationUserId == user.Id);

                if (originalTicket == null)
                    return RedirectToAction("Checkout", new { ticketId });

                paidAmount = ticket.Price;

                // Produžiti ExpiresAt originalnog ticketa za razliku između ExpiresAt extension ticketa i njegovog IssuedAt
                var extensionDuration = ticket.ExpiresAt.HasValue
                    ? ticket.ExpiresAt.Value - ticket.IssuedAt
                    : TimeSpan.Zero;

                originalTicket.ExpiresAt = (originalTicket.ExpiresAt ?? DateTime.Now) + extensionDuration;
                originalTicket.ExpirationReminderSent = false;

                // Označiti extension ticket kao plaćen i povezati ga sa originalnim
                ticket.IsPaid = true;
                ticket.PaidAt = DateTime.Now;
                ticket.ReservationCode = reservationCode;

                // Koristiti parkingName i ExpiresAt originalnog ticketa u emailu
                string parkingNameExt = originalTicket.ParkingObject?.name
                    ?? ticket.ParkingObject?.name
                    ?? "ParkirajBa Parking";
                string userEmailExt = user.Email!;
                string fullNameExt = user.FullName ?? cardName;

                await _database.SaveChangesAsync();

                try
                {
                    using var mail = new MailMessage();
                    mail.From = new MailAddress(_senderEmail, "ParkirajBa");
                    mail.To.Add(userEmailExt);
                    mail.Subject = $"Potvrda produženja rezervacije - {reservationCode}";
                    mail.Body = $"Poštovani/a {fullNameExt},\n\n" +
                                $"Vaše produženje rezervacije je uspješno obrađeno.\n\n" +
                                $"Kod produženja: {reservationCode}\n" +
                                $"Parking: {parkingNameExt}\n" +
                                $"Plaćeni iznos: {paidAmount:0.00} KM\n" +
                                $"Nova važnost do: {(originalTicket.ExpiresAt.HasValue ? originalTicket.ExpiresAt.Value.ToString("dd.MM.yyyy HH:mm") : "—")}\n\n" +
                                $"Hvala što koristite ParkirajBa!";
                    mail.IsBodyHtml = false;

                    using var qrGenerator = new QRCodeGenerator();
                    using var qrData = qrGenerator.CreateQrCode(originalTicket.ReservationCode ?? reservationCode, QRCodeGenerator.ECCLevel.Q);
                    using var qrCode = new PngByteQRCode(qrData);
                    byte[] qrBytes = qrCode.GetGraphic(20);

                    using var ms = new MemoryStream(qrBytes);
                    var attachment = new Attachment(ms, "ParkirajBa-QR.png", "image/png");
                    mail.Attachments.Add(attachment);

                    using var smtp = new SmtpClient("smtp.gmail.com", 587);
                    smtp.Credentials = new NetworkCredential(_senderEmail, _senderPassword);
                    smtp.EnableSsl = true;
                    smtp.Send(mail);
                }
                catch (Exception ex)
                {
                    TempData["EmailError"] = "Produženje potvrđeno, ali email nije mogao biti poslan: " + ex.Message;
                }

                return RedirectToAction("Success", new { code = reservationCode, ticketId = originalTicketId });
            }
            else if (isAdditionalCharge)
            {
                paidAmount = ticket.AdditionalCharge;

                ticket.TotalAdditionalChargesPaid += ticket.AdditionalCharge;

                ticket.AdditionalCharge = 0;

                ticket.AdditionalChargePaid = true;

                ticket.QrCodeActive = true;

                ticket.ExpiresAt = DateTime.Now.AddMinutes(15);

                ticket.ReservationCode = reservationCode;

                ticket.OverstayEmailSent = false;
            }
            else
            {
                ticket.IsPaid = true;

                ticket.PaidAt = DateTime.Now;

                ticket.ReservationCode = reservationCode;

                var parking = await _parkingRepository.GetByIdAsync(ticket.ParkingObjectId);
                if (parking != null && parking.availableSpots > 0)
                    parking.availableSpots--;
            }

            await _database.SaveChangesAsync();
            //Damir changes

            string parkingName = ticket.ParkingObject?.name ?? "ParkirajBa Parking";
            string userEmail = user.Email!;
            string fullName = user.FullName ?? cardName;

            try
            {
                using var mail = new MailMessage();
                mail.From = new MailAddress(_senderEmail, "ParkirajBa");
                mail.To.Add(userEmail);
                mail.Subject = $"Potvrda rezervacije - {reservationCode}";
                mail.Body = $"Poštovani/a {fullName},\n\n" +
                            $"Vaše plaćanje je uspješno obrađeno.\n\n" +
                            $"Kod rezervacije: {reservationCode}\n" +
                            $"Parking: {parkingName}\n" +
                            $"Plaćeni iznos: {paidAmount:0.00} KM\n" +
                            $"Vrijedi do: {(ticket.ExpiresAt.HasValue ? ticket.ExpiresAt.Value.ToString("dd.MM.yyyy HH:mm") : "—")}\n\n" +
                            $"Molimo pokažite priloženi QR kod na ulazu u parking.\n\n" +
                            $"Hvala što koristite ParkirajBa!";
                mail.IsBodyHtml = false;

                using var qrGenerator = new QRCodeGenerator();
                using var qrData = qrGenerator.CreateQrCode(reservationCode, QRCodeGenerator.ECCLevel.Q);
                using var qrCode = new PngByteQRCode(qrData);
                byte[] qrBytes = qrCode.GetGraphic(20);

                using var ms = new MemoryStream(qrBytes);
                var attachment = new Attachment(ms, "ParkirajBa-QR.png", "image/png");
                mail.Attachments.Add(attachment);

                using var smtp = new SmtpClient("smtp.gmail.com", 587);
                smtp.Credentials = new NetworkCredential(_senderEmail, _senderPassword);
                smtp.EnableSsl = true;
                smtp.Send(mail);
            }
            catch (Exception ex)
            {
                TempData["EmailError"] = "Plaćanje potvrđeno, ali email nije mogao biti poslan: " + ex.Message;
            }

            return RedirectToAction("Success", new { code = reservationCode, ticketId });
        }

        // GET: /Payment/Success
        [HttpGet]
        public async Task<IActionResult> Success(string code, int ticketId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "User");

            var ticket = await _parkingRepository.GetTicketByIdAsync(ticketId, user.Id);
            ViewBag.ReservationCode = code;
            ViewBag.Ticket = ticket;
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GenerateQr(int ticketId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized();

            var ticket = await _parkingRepository.GetTicketByIdAsync(ticketId, user.Id);

            if (ticket == null || string.IsNullOrEmpty(ticket.ReservationCode))
                return NotFound();

            using var qrGenerator = new QRCodeGenerator();
            using var qrData = qrGenerator.CreateQrCode(
                ticket.ReservationCode,
                QRCodeGenerator.ECCLevel.Q);

            using var qrCode = new PngByteQRCode(qrData);

            byte[] qrBytes = qrCode.GetGraphic(20);

            return File(qrBytes, "image/png");
        }


    }
}