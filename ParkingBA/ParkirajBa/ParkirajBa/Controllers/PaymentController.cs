using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Net;
using System.Net.Mail;

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
            string korisnikEmail = "unesitesvojmail@gmail.com";

            try
            {
                using (MailMessage mail = new MailMessage())
                {
                    mail.From = new MailAddress(mojEmail);
                    mail.To.Add(korisnikEmail);
                    mail.Subject = "Potvrda rezervacije - ParkirajBa";
                    mail.Body = $"Poštovani/a {ime},\n\nVaša uplata je uspješna. Pristup parkingu vam je omogućen.\n\nHvala što koristite ParkirajBa!";
                    mail.IsBodyHtml = false;

                    using (SmtpClient smtp = new SmtpClient("smtp.gmail.com", 587))
                    {
                        smtp.Credentials = new NetworkCredential(mojEmail, mojaLozinka);
                        smtp.EnableSsl = true;
                        smtp.Send(mail);
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