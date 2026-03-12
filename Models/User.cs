namespace greenSpotApi.Models
{
    public class User
    {
        public long UserId { get; set; } // EF Core automatically makes this the Primary Key
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty; // change to string
    }
}
