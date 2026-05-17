using Microsoft.AspNetCore.Identity;

//NE KORISTIMO

namespace ParkirajBa.Models
{
    public class RegisteredUser : IdentityUser
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }

        public string FullName => FirstName + " " + LastName;
    }
}