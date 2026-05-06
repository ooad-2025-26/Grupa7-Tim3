namespace ParkirajBa.Models
{
    public enum AccessLevel
    {
        Moderator,
        Owner
    }
    public class Administrator : User
    {
        public AccessLevel accessLevel { get; set; }
        public Administrator() : base()
        {
            accessLevel = AccessLevel.Moderator;
        }
    }
}
