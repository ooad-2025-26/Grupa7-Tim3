using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;

namespace ParkirajBa.Models
{
    public class ParkingObject
    {
        [Key]
        public int ID { get; set; }

        [Required(ErrorMessage = "Naziv parkinga je obavezan.")]
        public string name { get; set; }

        public string address { get; set; }
        public double latitude { get; set; }
        public double longitude { get; set; }

        // VALIDACIJE ZA UKUPAN BROJ MJESTA
        [Required(ErrorMessage = "Ukupan broj parking mjesta je obavezan.")]
        [Range(1, 10000, ErrorMessage = "Broj parking mjesta mora biti veći od 0.")]
        [RegularExpression(@"^\d+$", ErrorMessage = "Format nije ispravan. Dozvoljeni su samo cijeli pozitivni brojevi.")]
        public int? totalSpots { get; set; }

        /// <summary>
        /// Slobodna mjesta se računaju dinamički u controlleru/servisu.
        /// Ovo polje se ažurira pri kreiranju i otkazivanju rezervacije.
        /// </summary>
        // --- DODANE VALIDACIJE ZA SLOBODNA MJESTA ---
        [Range(0, 10000, ErrorMessage = "Broj slobodnih mjesta ne može biti negativan.")]
        [RegularExpression(@"^\d+$", ErrorMessage = "Format slobodnih mjesta mora biti cijeli broj.")]
        public int availableSpots { get; set; }

        public bool? hasCameras { get; set; }
        public bool? isDisabledAccessible { get; set; }
        public bool? hasEVCharger { get; set; }
        public double? maxHeight { get; set; }
        public bool? isUnderground { get; set; }
        public DateTime? opensAt { get; set; }
        public DateTime? closesAt { get; set; }

        public string? OwnerId { get; set; }

        [ValidateNever]
        public ApplicationUser Owner { get; set; }

        [ValidateNever]
        public ICollection<Pricing> Pricings { get; set; } = new List<Pricing>();

        public ParkingObject()
        {
        }
    }
}
