using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using MovieMunch.Context;
using MovieMunch.Registration.Models;
using System.Data;
using System.Data.SqlClient;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace MovieMunch.Registration
{
    [Route("api/[controller]")]
    [ApiController]
    public class RegistrationController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly IMediator _mediator;
        private readonly MOMContext _momContext;

        public RegistrationController(IConfiguration configuration, IMediator mediator ,MOMContext mOMContext)
        {
            _configuration = configuration;
            _mediator = mediator;
            _momContext = mOMContext;
        }

        [HttpPost]
        [Route("")]
        public async Task<IActionResult> RegistrationAsync(GetUserRegisterCommand registration)
        {
            if(string.IsNullOrEmpty(registration.Username) || string.IsNullOrEmpty(registration.Password))
            {
                return BadRequest("Invalid Data");
            }
            string result = await _mediator.Send(registration);

            if (result != "Failed to insert data")
            {
                return Ok(result);
            }

            return StatusCode(500, "Failed to insert data");
        }

        //[HttpGet]
        //[Route("checkUserName")]
        //public IActionResult CheckUsernameExists(string username)
        //{
        //    string connectionString = _configuration.GetConnectionString("mom");

        //    using (SqlConnection con = new SqlConnection(connectionString))
        //    {
        //        con.Open();

        //        string query = "SELECT COUNT(*) FROM Tblusermaster (nolock) WHERE UserName = @UserName";

        //        using (SqlCommand cmd = new SqlCommand(query, con))
        //        {
        //            cmd.Parameters.AddWithValue("@UserName", username);

        //            try
        //            {
        //                int count = (int)cmd.ExecuteScalar();
        //                con.Close();

        //                var response = new ValidateUsernameResponse
        //                {
        //                    IsAvailable = count == 0,
        //                    Message = count > 0 ? "Username already exists" : "Username is available"
        //                };

        //                return Ok(response);
        //            }
        //            catch (Exception ex)
        //            {
        //                con.Close();
        //                return StatusCode(500, new ValidateUsernameResponse
        //                {
        //                    IsAvailable = false,
        //                    Message = "Failed to check username"
        //                });
        //            }
        //        }
        //    }
        //}

        [HttpGet]
        [Route("checkUserName")]
        public IActionResult CheckUsernameExists(string username)
        {
            if (string.IsNullOrEmpty(username))
            {
                return BadRequest("Username cannot be null or empty.");
            }

            var count = _momContext.Users.Count(u => u.UserName == username);

            var response = new ValidateUsernameResponse
            {
                IsAvailable = count == 0,
                Message = count > 0 ? "Username already exists" : "Username is available"
            };

            return Ok(response);
        }



        [HttpPost]
        [Route("login")]
        public IActionResult Login(LoginModel credentials)
        {
            string connectionString = _configuration.GetConnectionString("mom");
            using (SqlConnection con = new SqlConnection(connectionString))
            {
                string query = !string.IsNullOrEmpty(credentials.PhoneNumber)
                    ? "SELECT Password FROM Tblusermaster WHERE PhoneNumber = @PhoneNumber"
                    : "SELECT Password FROM Tblusermaster WHERE Email = @Email";

                SqlCommand cmd = new SqlCommand(query, con);

                if (!string.IsNullOrEmpty(credentials.PhoneNumber))
                    cmd.Parameters.AddWithValue("@PhoneNumber", credentials.PhoneNumber);
                else
                    cmd.Parameters.AddWithValue("@Email", credentials.Email);

                con.Open();
                string storedHashedPassword = (string)cmd.ExecuteScalar();

                if (storedHashedPassword != null && BCrypt.Net.BCrypt.Verify(credentials.Password, storedHashedPassword))
                {
                    var token = GenerateJwtToken(credentials);
                    return Ok(new { Token = token });
                }
                else
                {
                    return Unauthorized("Invalid User");
                }
            }
        }

        private string GenerateJwtToken(LoginModel user)
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Email ?? user.PhoneNumber),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.Now.AddMinutes(30),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
