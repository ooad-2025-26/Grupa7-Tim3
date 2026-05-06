using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ParkirajBa.Models
{
    public class RegisteredUser: User
    {
        public string phoneNumber { get; set; }
        public RegisteredUser():base()
        {
            phoneNumber = string.Empty;
        }
    }
}
