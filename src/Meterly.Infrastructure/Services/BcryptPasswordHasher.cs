using Meterly.Application.Common.Interfaces;
using BC = BCrypt.Net.BCrypt;

namespace Meterly.Infrastructure.Services;

public class BcryptPasswordHasher : IPasswordHasher
{
    public string HashPassword(string password)
    {
        // Generate a salt and hash the password (default work factor is 11 in this library)
        return BC.HashPassword(password);
    }

    public bool VerifyPassword(string password, string hashedPassword)
    {
        return BC.Verify(password, hashedPassword);
    }
}
