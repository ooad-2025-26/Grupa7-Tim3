using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Net;
using System.Net.Mail;
using QRCoder;
using System.IO;
using ParkirajBa.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;

namespace ParkirajBa.Controllers
{
    [Authorize] 
    public class PaymentController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly string _parkingBaEmail;
        private readonly string _parkingBaEmailConnection;

        public PaymentController(
            UserManager<ApplicationUser> userManager,
            IConfiguration configuration)
        {
            _userManager = userManager;
            _parkingBaEmail = configuration["EmailSettings:SenderEmail"]
                ?? throw new InvalidOperationException("EmailSettings:SenderEmail nije konfigurisan.");
            _parkingBaEmailConnection = configuration["EmailSettings:AppPassword"]
                ?? throw new InvalidOperationException("EmailSettings:AppPassword nije konfigurisan.");
        }

        public IActionResult Placanje() => View();

        public IActionResult Potvrda(string ime)
        {
            ViewBag.ImeKorisnika = ime;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> PosaljiEmail()
        {
            var currentUser = await _userManager.GetUserAsync(User);

            if (currentUser == null)
                return Unauthorized();

            string userEmail = currentUser.Email;
            string fullName  = currentUser.FullName;

            string uniqueCode = "PB-" + Guid.NewGuid().ToString().Substring(0, 8).ToUpper();

            try
            {
                using (MailMessage mail = new MailMessage())
                {
                    mail.From = new MailAddress(_parkingBaEmail);
                    mail.To.Add(userEmail);
                    mail.Subject = "Potvrda rezervacije - ParkirajBa";
                    mail.Body    = $"Poštovani/a {fullName},\n\nVaša uplata je uspješna. U prilogu se nalazi Vaš QR kod za pristup parkingu.\n\nKod rezervacije: {uniqueCode}\n\nHvala što koristite ParkirajBa!";
                    mail.IsBodyHtml = false;

                    using (QRCodeGenerator qrGenerator = new QRCodeGenerator())
                    using (QRCodeData qrCodeData = qrGenerator.CreateQrCode(uniqueCode, QRCodeGenerator.ECCLevel.Q))
                    using (PngByteQRCode qrCode = new PngByteQRCode(qrCodeData))
                    {
                        byte[] qrCodeBytes = qrCode.GetGraphic(20);

                        using (MemoryStream ms = new MemoryStream(qrCodeBytes))
                        {
                            Attachment attachment = new Attachment(ms, "ParkirajBa-QR.png", "image/png");
                            mail.Attachments.Add(attachment);

                            using (SmtpClient smtp = new SmtpClient("smtp.gmail.com", 587))
                            {
                                smtp.Credentials = new NetworkCredential(_parkingBaEmail, _parkingBaEmailConnection);
                                smtp.EnableSsl   = true;
                                smtp.Send(mail);
                            }
                        }
                    }
                }
                return RedirectToAction("Uspjeh");
            }
            catch (Exception ex)
            {
                return Content("Greška pri slanju maila: " + ex.Message);
            }
        }

        public IActionResult Uspjeh() => View();
    }
}
