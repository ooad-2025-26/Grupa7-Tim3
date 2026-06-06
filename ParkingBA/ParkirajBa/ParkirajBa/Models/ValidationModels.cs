using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace ParkirajBa.Models
{
    // 1. REGISTRACIJA KORISNIKA
    public class ExtendedRegisterViewModel
    {
        [Required(ErrorMessage = "Ime je obavezno.")]
        [StringLength(50, MinimumLength = 2, ErrorMessage = "Ime mora imati između 2 i 50 karaktera.")]
        [RegularExpression(@"^[a-zA-ZčćžšđČĆŽŠĐ\s\-]+$", ErrorMessage = "Ime može sadržavati samo slova, razmak i crticu.")]
        public string Ime { get; set; }

        [Required(ErrorMessage = "Prezime je obavezno.")]
        [StringLength(50, MinimumLength = 2, ErrorMessage = "Prezime mora imati između 2 i 50 karaktera.")]
        [RegularExpression(@"^[a-zA-ZčćžšđČĆŽŠĐ\s\-]+$", ErrorMessage = "Prezime može sadržavati samo slova, razmak i crticu.")]
        public string Prezime { get; set; }


        [Required(ErrorMessage = "Email adresa je obavezna.")]
        [EmailAddress(ErrorMessage = "Unesite ispravan format email adrese.")]
        [StringLength(100, ErrorMessage = "Email ne može biti duži od 100 karaktera.")]
        public string Email { get; set; }


        [Required(ErrorMessage = "Lozinka je obavezna.")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "Lozinka mora imati najmanje 8 karaktera.")]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&.#\-_])[A-Za-z\d@$!%*?&.#\-_]{8,}$",
            ErrorMessage = "Lozinka mora sadržavati najmanje jedno veliko slovo, jedno malo slovo, jedan broj i jedan specijalni znak.")]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [Required(ErrorMessage = "Potvrda lozinke je obavezna.")]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Potvrda lozinke se ne poklapa sa unesenom lozinkom.")]
        public string ConfirmPassword { get; set; }
    }

    // 2. KREIRANJE I EDITOVANJE REZERVACIJE
    public class ReservationViewModel : IValidatableObject
    {
        public int ParkingObjectId { get; set; }

        [Required(ErrorMessage = "Registracijska oznaka je obavezna.")]
        [RegularExpression(@"^[a-zA-Z0-9\-]+$", ErrorMessage = "Registracija može sadržavati samo slova, brojeve i crticu.")]
        public string PlateNumber { get; set; }

        [Required(ErrorMessage = "Datum i vrijeme početka su obavezni.")]
        public DateTime StartTime { get; set; }

        [Required(ErrorMessage = "Datum i vrijeme završetka su obavezni.")]
        public DateTime EndTime { get; set; }

        [Range(0.1, 720.0, ErrorMessage = "Trajanje rezervacije mora biti veće od nule.")]
        public double DurationHours { get; set; }

        [StringLength(500, ErrorMessage = "Komentar/opis ne smije biti duži od 500 znakova.")]
        public string Comment { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (!string.IsNullOrEmpty(PlateNumber))
                PlateNumber = PlateNumber.Trim().ToUpper();

            if (StartTime < DateTime.Now.AddMinutes(-5))
            {
                yield return new ValidationResult("Nije dozvoljeno rezervisati termin koji je već prošao.", new[] { nameof(StartTime) });
            }

            if (EndTime <= StartTime)
            {
                yield return new ValidationResult("Vrijeme završetka mora biti nakon vremena početka.", new[] { nameof(EndTime) });
            }
        }
    }

    // 3. SIGURNO PLAĆANJE KARTICOM
    public class CardPaymentViewModel : IValidatableObject
    {
        [Required]
        public int TicketId { get; set; }

        [Required(ErrorMessage = "Ime na kartici je obavezno.")]
        [StringLength(100, MinimumLength = 5, ErrorMessage = "Unesite puno ime i prezime vlasnika kartice.")]
        [RegularExpression(@"^[a-zA-ZčćžšđČĆŽŠĐ\s]+$", ErrorMessage = "Ime može sadržavati samo slova i razmake.")]
        public string CardName { get; set; }

        [Required(ErrorMessage = "Broj kartice je obavezan.")]
        [RegularExpression(@"^[0-9\s\-]+$", ErrorMessage = "Broj kartice može sadržavati samo cifre, razmake i crtice.")]
        public string CardNumber { get; set; }

        [Required(ErrorMessage = "Datum isteka je obavezan (MM/YY).")]
        [RegularExpression(@"^(0[1-9]|1[0-2])\/([0-9]{2})$", ErrorMessage = "Format isteka kartice mora biti MM/YY.")]
        public string ExpiryDate { get; set; }

        [Required(ErrorMessage = "CVV je obavezan.")]
        [RegularExpression(@"^[0-9]{3,4}$", ErrorMessage = "CVV mora sadržavati tačno 3 ili 4 cifre.")]
        public string CVV { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            string cleanCard = CardNumber.Replace(" ", "").Replace("-", "");
            if (cleanCard.Length < 13 || cleanCard.Length > 19 || !PassesLuhnCheck(cleanCard))
            {
                yield return new ValidationResult("Broj kreditne kartice nije validan.", new[] { nameof(CardNumber) });
            }

            var match = Regex.Match(ExpiryDate, @"^(?<month>\d{2})\/(?<year>\d{2})$");
            if (match.Success)
            {
                int month = int.Parse(match.Groups["month"].Value);
                int year = 2000 + int.Parse(match.Groups["year"].Value);

                var lastDayOfMonth = new DateTime(year, month, DateTime.DaysInMonth(year, month), 23, 59, 59);
                if (lastDayOfMonth < DateTime.UtcNow)
                {
                    yield return new ValidationResult("Kreditna kartica je istekla.", new[] { nameof(ExpiryDate) });
                }
            }

            yield break;
        }

        private bool PassesLuhnCheck(string cardNumber)
        {
            int sum = 0; bool alternate = false;
            for (int i = cardNumber.Length - 1; i >= 0; i--)
            {
                int nx = int.Parse(cardNumber[i].ToString());
                if (alternate) { nx *= 2; if (nx > 9) nx -= 9; }
                sum += nx; alternate = !alternate;
            }
            return sum % 10 == 0;
        }
    }
}