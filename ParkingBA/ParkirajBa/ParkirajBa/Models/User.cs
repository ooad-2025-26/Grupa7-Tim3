using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace ParkirajBa.Models
{
    public abstract class User
    {
        [Key]
        public int ID { get; set; }
        public string name { get; set; }
        public string surname { get; set; }
        public string email { get; set; }
        public string passwordHash { get; set; }

        public User()
        {
            name = string.Empty;
            surname = string.Empty;
            email = string.Empty;
            passwordHash = string.Empty;
        }
    }
}
