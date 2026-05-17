using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using static QRCoder.PayloadGenerator;

namespace ParkirajBa.Models
{
    public class ApplicationUser : IdentityUser
    {
        [Required]
        [MaxLength(50)]
        public string FirstName { get; set; }

        [Required]
        [MaxLength(50)]
        public string LastName { get; set; }

        //kad je kreiran 
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ApplicationUser()
        {
        }

        // pomocna metoda da prikaze ime i prezime
        public string FullName => $"{FirstName} {LastName}";
    }
}