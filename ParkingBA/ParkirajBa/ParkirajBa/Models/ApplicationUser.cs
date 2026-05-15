using Microsoft.AspNetCore.Identity;
using static QRCoder.PayloadGenerator;

namespace ParkirajBA.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }

        public ApplicationUser() : base()
        {
        }
    }
}