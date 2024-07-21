using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
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

        public RegistrationController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpPost]
        [Route("")]
        public IActionResult Registration(RegistrationModel registration)
        {
            string connectionString = _configuration.GetConnectionString("mom");

            // Hash the password before storing it
            string hashedPassword = BCrypt.Net.BCrypt.HashPassword(registration.Password);

            using (SqlConnection con = new SqlConnection(connectionString))
            {
                con.Open();

                using (SqlCommand cmd = new SqlCommand("UserRegistration", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@UserName", registration.Username);
                    cmd.Parameters.AddWithValue("@Password", hashedPassword); // Store the hashed password
                    cmd.Parameters.AddWithValue("@Email", registration.Email);
                    cmd.Parameters.AddWithValue("@PhoneNumber", registration.PhoneNumber);
                    cmd.Parameters.AddWithValue("@IsActive", registration.IsActive);

                    try
                    {
                        SqlDataReader reader = cmd.ExecuteReader();
                        if (reader.Read())
                        {
                            string result = reader["Result"].ToString();
                            con.Close();
                            return Ok(result);
                        }
                    }
                    catch (Exception ex)
                    {
                        con.Close();
                        return StatusCode(500, "Failed to insert data");
                    }
                }

                con.Close();
                return StatusCode(500, "Failed to insert data");
            }
        }

        [HttpPost]
        [Route("login")]
        public IActionResult Login(RegistrationModel registration)
        {
            string connectionString = _configuration.GetConnectionString("mom");
            using (SqlConnection con = new SqlConnection(connectionString))
            {
                string query = !string.IsNullOrEmpty(registration.PhoneNumber)
                    ? "SELECT Password FROM Registration WHERE PhoneNumber = @PhoneNumber"
                    : "SELECT Password FROM Registration WHERE Email = @Email";

                SqlCommand cmd = new SqlCommand(query, con);

                if (!string.IsNullOrEmpty(registration.PhoneNumber))
                    cmd.Parameters.AddWithValue("@PhoneNumber", registration.PhoneNumber);
                else
                    cmd.Parameters.AddWithValue("@Email", registration.Email);

                con.Open();
                string storedHashedPassword = (string)cmd.ExecuteScalar();

                if (storedHashedPassword != null && BCrypt.Net.BCrypt.Verify(registration.Password, storedHashedPassword))
                {
                    var token = GenerateJwtToken(registration);
                    return Ok(new { Token = token });
                }
                else
                {
                    return Unauthorized("Invalid User");
                }
            }
        }

        private string GenerateJwtToken(RegistrationModel user)
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
