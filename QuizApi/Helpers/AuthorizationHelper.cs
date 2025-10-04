using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.IdentityModel.Tokens;
using QuizApi.Models.Identity;
using QuizApi.Settings;

namespace QuizApi.Helpers
{
    public class AuthorizationHelper
    {
        public static string GenerateRandomAlphaNumeric()
        {
            byte[] randomNumber = new byte[32];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            string token = Convert.ToBase64String(randomNumber);

            var result = Regex.Replace(token, "[^a-zA-Z0-9]", string.Empty);

            return result;
        }

        public static string GenerateJWTToken(JWTSetting jwtSettings, DateTime tokenExpiredTime, UserModel user)
        {
            var secureKey = Encoding.UTF8.GetBytes(jwtSettings.Key);
            var securityKey = new SymmetricSecurityKey(secureKey);
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha512);

            var jwtTokenHandler = new JwtSecurityTokenHandler();

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                    {
                        new Claim(ClaimTypes.Name, user.UserId),
                        new Claim("nama", user.Username),
                        new Claim("userId", user.UserId)
                    }
                ),
                Expires = tokenExpiredTime,
                NotBefore = DateTime.UtcNow,
                Audience = jwtSettings.Audience,
                Issuer = jwtSettings.Issuer,
                SigningCredentials = credentials
            };

            var token = jwtTokenHandler.CreateToken(tokenDescriptor);
            var jwtToken = jwtTokenHandler.WriteToken(token);

            return jwtToken;
        }
    }
}