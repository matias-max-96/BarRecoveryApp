using System.Security.Cryptography;

namespace BarRecoveryApp.Infrastructure.Persistence.Seed
{
    public class PinHashHelper
    {
        private const int SaltSize = 16;
        private const int HashSize = 32;
        private const int Iterations = 200_000;
        
        public static (string Hash, string Salt) CreateHash(string pin)
        {
            byte[] saltBytes = RandomNumberGenerator.GetBytes(SaltSize);

            byte[] pbkdf2 = Rfc2898DeriveBytes.Pbkdf2(
                pin,
                saltBytes,
                Iterations,
                HashAlgorithmName.SHA256,
                HashSize
                );

            return (
                Convert.ToBase64String(pbkdf2),
                Convert.ToBase64String(saltBytes)
                );
        }
        public static bool VerifyPin(string pin, string storedHash, string storedSalt)
        {
            byte[] saltBytes = Convert.FromBase64String(storedSalt);
            byte[] hashToCompare = Convert.FromBase64String(storedHash);

            byte[] newHash = Rfc2898DeriveBytes.Pbkdf2(
                pin,
                saltBytes,
                Iterations,
                HashAlgorithmName.SHA256,
                HashSize
                );

            return CryptographicOperations.FixedTimeEquals(newHash, hashToCompare);
            
        }
    }
}
