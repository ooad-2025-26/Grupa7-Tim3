using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ParkirajBa.Models
{
    public class Owner: User
    {
        public string companyName { get; set; }
        public Owner():base()
        {
            companyName = string.Empty;
        }
    }
}
