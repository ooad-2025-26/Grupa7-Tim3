using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;

namespace ParkirajBa.Models
{
    public class ParkingRequest
    {
        [Key]
        public int ID {  get; set; }

        public DateTime TimeSent {  get; set; }

        [ForeignKey("ParkingObject")]
        public int ParkingID {  get; set; }


        public ParkingRequest() { }
    }
}
