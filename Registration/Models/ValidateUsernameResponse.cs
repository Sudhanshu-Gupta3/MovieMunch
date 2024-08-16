namespace MovieMunch.Registration.Models
{
    public class ValidateUsernameResponse
    {
        public bool IsAvailable { get; set; }
        public string Message { get; set; }
    }
}
