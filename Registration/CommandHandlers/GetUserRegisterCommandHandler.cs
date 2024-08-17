using MediatR;
using MovieMunch.Registration.Models;
using System.Data.SqlClient;
using System.Data;

namespace MovieMunch.Registration.CommandHandlers
{
    public class GetUserRegisterCommandHandler: IRequestHandler<GetUserRegisterCommand, string>
    {
        private readonly IConfiguration _configuration;

        public GetUserRegisterCommandHandler(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task<string> Handle(GetUserRegisterCommand request, CancellationToken cancellationToken)
        {
            string connectionString = _configuration.GetConnectionString("mom");
            string hashedPassword = BCrypt.Net.BCrypt.HashPassword(request.Password);

            using (SqlConnection con = new SqlConnection(connectionString))
            {
                con.Open();

                using (SqlCommand cmd = new SqlCommand("UserRegistration", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@UserName", request.Username);
                    cmd.Parameters.AddWithValue("@Password", hashedPassword);
                    cmd.Parameters.AddWithValue("@Email", request.Email);
                    cmd.Parameters.AddWithValue("@PhoneNumber", request.PhoneNumber);
                    cmd.Parameters.AddWithValue("@IsActive", request.IsActive);

                    try
                    {
                        SqlDataReader reader = await cmd.ExecuteReaderAsync(cancellationToken);
                        if (await reader.ReadAsync(cancellationToken))
                        {
                            return reader["Result"].ToString();
                        }
                    }
                    catch (Exception ex)
                    {
                        throw new Exception("Failed to insert data", ex);
                    }
                }
                return "Failed to insert data";
            }
        }

    }
}
