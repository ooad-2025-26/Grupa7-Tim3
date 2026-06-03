using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;

namespace ParkirajBa.Models
{
    public class ParkingObject
    {
        [Key]
        public int ID { get; set; }
        [Required]
        public string name { get; set; }
        public string address { get; set; }
        public double latitude { get; set; }
        public double longitude { get; set; }
        public int? totalSpots { get; set; }

        /// <summary>
        /// Slobodna mjesta se računaju dinamički u controlleru/servisu.
        /// Ovo polje se ažurira pri kreiranju i otkazivanju rezervacije.
        /// </summary>
        public int availableSpots { get; set; }

        public bool? hasCameras { get; set; }
        public bool? isDisabledAccessible { get; set; }
        public bool? hasEVCharger { get; set; }
        public double? maxHeight { get; set; }
        public bool? isUnderground { get; set; }
        public DateTime? opensAt { get; set; }
        public DateTime? closesAt { get; set; }

        public string? OwnerId { get; set; }

        public ApplicationUser Owner { get; set; }

        public ICollection<Pricing> Pricings { get; set; } = new List<Pricing>();


        public ParkingObject() { 

        }
    }
}
