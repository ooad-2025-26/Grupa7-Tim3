using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using static QRCoder.PayloadGenerator;

namespace ParkirajBa.Models
{
    public class ApplicationUser : IdentityUser
    {
        [Required(ErrorMessage = "Ime je obavezno.")]
        [MinLength(2, ErrorMessage = "Ime mora imati najmanje 2 slova.")]
        [MaxLength(50, ErrorMessage = "Ime ne smije biti duže od 50 slova.")]
        [RegularExpression(@"^[a-zA-ZčćžšđČĆŽŠĐ\s\-]+$", ErrorMessage = "Ime može sadržavati samo slova, razmak i crticu.")]
        public string FirstName { get; set; }

        [Required(ErrorMessage = "Prezime je obavezno.")]
        [MinLength(2, ErrorMessage = "Prezime mora imati najmanje 2 slova.")]
        [MaxLength(50, ErrorMessage = "Prezime ne smije biti duže od 50 slova.")]
        [RegularExpression(@"^[a-zA-ZčćžšđČĆŽŠĐ\s\-]+$", ErrorMessage = "Prezime može sadržavati samo slova, razmak i crticu.")]
        public string LastName { get; set; }

        //date of creation of the user account
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ApplicationUser()
        {
        }

        // return full name of the user
        public string FullName => $"{FirstName} {LastName}";
    }
}