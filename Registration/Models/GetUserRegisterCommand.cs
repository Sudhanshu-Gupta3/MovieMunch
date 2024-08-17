using MediatR;

namespace MovieMunch.Registration.Models
{
    public class GetUserRegisterCommand: IRequest<string>
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public int? IsActive { get; set; }
    }
}
