using System.Security.Cryptography;
using Microsoft.Extensions.Options;

namespace QuizApi.Helpers
{
    public sealed class HashingOptions
    {
        public int Iterations { get; set; } = 10000;
    }

    public interface IPasswordHasher
    {
        string Hash(string password);
        // (bool Verified, bool NeedsUpgrade) Check(string hash, string password);
    }
    public sealed class PasswordHasherHelper : IPasswordHasher
    {
        private const int SaltSize = 16;
        private const int KeySize = 32;
        public PasswordHasherHelper(IOptions<HashingOptions> options)
        {
            Options = options.Value;
        }

        public PasswordHasherHelper()
        {
            Options = new HashingOptions();
        }

        private HashingOptions Options { get; }

        public string Hash(string password)
        {
            // using, object "algorithm" automatically disposed outside the scope 
            // prevents memory leak
            // Rfc2898DeriveBytes: uses PBKDF2 algorithm
            using (var algorithm = new Rfc2898DeriveBytes(password, SaltSize, Options.Iterations, HashAlgorithmName.SHA512))
            {
                /**
                * key: final value of hashing algorithm after password and salt were proceeded several times
                * this is stored inside the database as hashed password
                */
                var key = Convert.ToBase64String(algorithm.GetBytes(KeySize));
                /**
                * salt: unique random value added to the password before being hashed
                * prevents rainbow table attack
                * creates unique hash for every user
                */
                var salt = Convert.ToBase64String(algorithm.Salt);

                return $"{salt}.{key}";
            }
        }

        public bool IsPasswordValid(string hash, string password)
        {
            var parts = hash.Split('.', 2);

            if (parts.Length != 2)
            {
                return false;
            }

            var iterations = Options.Iterations;
            var salt = Convert.FromBase64String(parts[0]);
            var key = Convert.FromBase64String(parts[1]);

            using (var algorithm = new Rfc2898DeriveBytes(
              password,
              salt,
              iterations,
              HashAlgorithmName.SHA512))
            {
                var keyToCheck = algorithm.GetBytes(KeySize);

                var isValid = keyToCheck.SequenceEqual(key);

                return isValid;
            }
        }
    }
}