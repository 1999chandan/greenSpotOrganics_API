namespace greenSpotApi.Models
{
    public class RegisterDto
    {
        public required string Username { get; set; }
        public required string Password { get; set; }
        public string Email { get; set; } = string.Empty;
    }
}
