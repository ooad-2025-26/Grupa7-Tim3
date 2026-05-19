using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Net;
using System.Net.Mail;
using QRCoder;
using System.IO;

namespace ParkirajBa.Controllers
{
    // [Authorize] checks if the user is authenticated before allowing access to the controller's actions. If the user is not authenticated, they will be redirected to the login page.
    public class PaymentController : Controller
    {
        private readonly string parkingBaEmail = "parkirajba.service@gmail.com";
        private readonly string parkingBaEmailConnection = "iplx fham rnwz oajz";

        public IActionResult Placanje() => View();

        public IActionResult Potvrda(string ime)
        {
            ViewBag.ImeKorisnika = ime;
            return View();
        }

        [HttpPost]
        public IActionResult PosaljiEmail(string ime)
        {
            // Email registered usera.
            // string korisnikEmail = User.Identity.Name; currently hardcoded for testing purposes. In a real application, this would be retrieved from the authenticated user's information
            string userEmail = "stavititestniemail@gmail.com";

            //generating unique code for the reservation, which will be included in the email and encoded in the QR code. This code can be used to verify the reservation at the parking lot.
            string uniqueCode = "PB-" + Guid.NewGuid().ToString().Substring(0, 8).ToUpper();

            try
            {
                using (MailMessage mail = new MailMessage())
                {
                    mail.From = new MailAddress(parkingBaEmail);
                    mail.To.Add(userEmail);
                    mail.Subject = "Potvrda rezervacije - ParkirajBa";
                    mail.Body = $"Poštovani/a {ime},\n\nVaša uplata je uspješna. U prilogu se nalazi Vaš QR kod za pristup parkingu.\n\nKod rezervacije: {unikatniKod}\n\nHvala što koristite ParkirajBa!";
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
                                smtp.Credentials = new NetworkCredential(parkingBaEmail, parkingBaEmailConnection);
                                smtp.EnableSsl = true;
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