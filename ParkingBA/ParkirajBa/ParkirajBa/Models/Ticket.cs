using ParkirajBa.Models;
using ParkirajBA.Models; // <-- veliko B, konzistentno sa ApplicationUser
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ParkirajBA.Models
{
    public class Ticket
    {
        [Key]
        public int Id { get; set; }

        public DateTime IssuedAt { get; set; } = DateTime.Now;

        public DateTime? ExpiresAt { get; set; }

        public decimal Price { get; set; }

        public string ApplicationUserId { get; set; }

        [ForeignKey("ApplicationUserId")]
        public ApplicationUser ApplicationUser { get; set; }

        public int ParkingObjectId { get; set; }

        [ForeignKey("ParkingObjectId")]
        public ParkingObject ParkingObject { get; set; }
    }
}