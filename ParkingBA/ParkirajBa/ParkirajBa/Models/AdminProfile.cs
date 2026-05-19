using ParkirajBa.Models;

namespace ParkirajBa.Models

{
    public class AdminProfile
    {
        public int Id { get; set; }

        public string Department { get; set; }

        public string UserId { get; set; }
        public ApplicationUser User { get; set; }
    }
}
