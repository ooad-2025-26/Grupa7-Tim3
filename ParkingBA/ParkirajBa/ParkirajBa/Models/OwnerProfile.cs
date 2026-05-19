using ParkirajBa.Models;

namespace ParkirajBa.Models
{
    public class OwnerProfile
    {
        public int Id { get; set; }

        public string CompanyName { get; set; }

        public string UserId { get; set; }
        public ApplicationUser User { get; set; }
    }
}