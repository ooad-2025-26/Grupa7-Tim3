using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Net;
using System.Net.Mail;
using QRCoder;
using System.IO;

namespace ParkirajBa.Controllers
{
    // [Authorize] Provjerava da li je korisnik ulogovan, zakomentarisano radi testiranja
    public class PaymentController : Controller
    {
        private readonly string mojEmail = "parkirajba.service@gmail.com";
        private readonly string mojaLozinka = "iplx fham rnwz oajz";

        public IActionResult Placanje() => View();

        public IActionResult Potvrda(string ime)
        {
            ViewBag.ImeKorisnika = ime;
            return View();
        }

        [HttpPost]
        public IActionResult PosaljiEmail(string ime)
        {
            // Email ulogovanog korisnika
            // string korisnikEmail = User.Identity.Name; zakomentarisano radi testiranja, zamijenjeno sa test emailom
            string korisnikEmail = "dzejjlaa@gmail.com";

            //Generisanje unikatnog ID-a za rampu
            string unikatniKod = "PB-" + Guid.NewGuid().ToString().Substring(0, 8).ToUpper();

            try
            {
                using (MailMessage mail = new MailMessage())
                {
                    mail.From = new MailAddress(mojEmail);
                    mail.To.Add(korisnikEmail);
                    mail.Subject = "Potvrda rezervacije - ParkirajBa";
                    mail.Body = $"Poštovani/a {ime},\n\nVaša uplata je uspješna. U prilogu se nalazi Vaš QR kod za pristup parkingu.\n\nKod rezervacije: {unikatniKod}\n\nHvala što koristite ParkirajBa!";
                    mail.IsBodyHtml = false;

                    using (QRCodeGenerator qrGenerator = new QRCodeGenerator())
                    using (QRCodeData qrCodeData = qrGenerator.CreateQrCode(unikatniKod, QRCodeGenerator.ECCLevel.Q))
                    using (PngByteQRCode qrCode = new PngByteQRCode(qrCodeData))
                    {
                        byte[] qrCodeBytes = qrCode.GetGraphic(20); 

                        
                        using (MemoryStream ms = new MemoryStream(qrCodeBytes))
                        {
                            Attachment attachment = new Attachment(ms, "ParkirajBa-QR.png", "image/png");
                            mail.Attachments.Add(attachment);

                            using (SmtpClient smtp = new SmtpClient("smtp.gmail.com", 587))
                            {
                                smtp.Credentials = new NetworkCredential(mojEmail, mojaLozinka);
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