using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ParkirajBa.Models
{
    public class ParkingObject
    {
        [Key]
        public int ID { get; set; }
        public string name { get; set; }
        public string address { get; set; }
        public double? latitude { get; set; }
        public double? longitude { get; set; }
        public int totalSpots { get; set; }
        public bool? hasCameras { get; set; }
        public bool? isDisabledAccessible { get; set; }
        public bool? hasEVCharger { get; set; }
        public double? maxHeight { get; set; }
        public bool? isUnderground { get; set; }
        public DateTime? opensAt { get; set; }
        public DateTime? closesAt { get; set; }
        //add owner ID foreign key...

        public List<Pricing> pricings { get; set; } = new List<Pricing>();

        public ParkingObject() { 
            address = string.Empty;
            name = string.Empty;
        }
    }
}
