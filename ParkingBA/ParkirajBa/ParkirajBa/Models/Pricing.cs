using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ParkirajBa.Models { 

    public enum PricingType
    {
        Hourly,
        Daily,
        Weekly,
        Monthly,
        Yearly,
        InitialFreebie
    }
    public class Pricing
    {
        [Key]
        public int ID { get; set; }
        public PricingType pricingType { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        public decimal price { get; set; }
        public DateTime? validFrom { get; set; }
        public DateTime? validTo { get; set; }

        [ForeignKey("ParkingObject")]
        public int ParkingObjectID { get; set; }
        public ParkingObject? parkingObject { get; set; }

        public Pricing()
        {
        }
    }
}
