using System.Security.Cryptography;
using System.Text;

namespace SchoolWise.Application.Verifications;

public static class VerificationCodeSecret
{
    public static string Create() => Convert.ToHexString(RandomNumberGenerator.GetBytes(24));
    public static string Hash(string code) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(code.Trim().ToUpperInvariant())));
}
