using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;

namespace ParkirajBa.Models
{
    public class ParkingImage
    {
        [Key]
        public int ID {  get; set; }

        public string ImagePath {  get; set; }

        [ForeignKey("ParkingObject")]
        public int ParkingObjectID {  get; set; }

        public ParkingImage() {
            
        }
    }
}
