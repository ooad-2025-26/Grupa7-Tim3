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

        // opcionalno: da znaš kad je user kreiran
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ApplicationUser()
        {
        }

        // pomoćno svojstvo (nije u bazi, samo za prikaz)
        public string FullName => $"{FirstName} {LastName}";
    }
}