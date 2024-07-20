using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
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
        public string registration(RegistrationModel registration)
        {
            string connectionString = _configuration.GetConnectionString("mom");

            using (SqlConnection con = new SqlConnection(connectionString))
            {
                con.Open();

                using (SqlCommand cmd = new SqlCommand("UserRegistration", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@UserName", registration.Username);
                    cmd.Parameters.AddWithValue("@Password", registration.Password);
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
                            return result;
                        }
                    }
                    catch (Exception ex)
                    {
                        con.Close();
                        return "Failed to insert data";
                    }
                }

                con.Close();
                return "Failed to insert data";
            }
        }

        [HttpPost]
        [Route("login")]
        public IActionResult Login(RegistrationModel registration)
        {
            string connectionString = _configuration.GetConnectionString("mom");
            using (SqlConnection con = new SqlConnection(connectionString))
            {
                string query = string.IsNullOrEmpty(registration.PhoneNumber)
                    ? "SELECT * FROM Registration WHERE Email = @Email AND Password = @Password"
                    : "SELECT * FROM Registration WHERE PhoneNumber = @PhoneNumber AND Password = @Password";

                SqlCommand cmd = new SqlCommand(query, con);

                if (string.IsNullOrEmpty(registration.PhoneNumber))
                    cmd.Parameters.AddWithValue("@Email", registration.Email);
                else
                    cmd.Parameters.AddWithValue("@PhoneNumber", registration.PhoneNumber);

                cmd.Parameters.AddWithValue("@Password", registration.Password);

                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                da.Fill(dt);

                if (dt.Rows.Count > 0)
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
