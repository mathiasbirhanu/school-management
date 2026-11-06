using System.Security.Cryptography;
using Scholaris.Application.Interfaces;

namespace Scholaris.Infrastructure.Identity;

/// <summary>PBKDF2 (SHA-256, 100k iterations) password hashing — no external dependency.</summary>
public class PasswordHasher : IPasswordHasher
{
    private const int Iterations = 100_000;
    private const int SaltSize = 16;
    private const int KeySize = 32;

    public string Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        using var derive = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA256);
        return $"{Iterations}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(derive.GetBytes(KeySize))}";
    }

    public bool Verify(string hash, string password)
    {
        var parts = hash.Split('.', 3);
        if (parts.Length != 3 || !int.TryParse(parts[0], out var iterations)) return false;
        byte[] salt, expected;
        try
        {
            salt = Convert.FromBase64String(parts[1]);
            expected = Convert.FromBase64String(parts[2]);
        }
        catch (FormatException) { return false; }

        using var derive = new Rfc2898DeriveBytes(password, salt, iterations, HashAlgorithmName.SHA256);
        var actual = derive.GetBytes(expected.Length);
        return CryptographicOperations.FixedTimeEquals(actual, expected);
    }
}
