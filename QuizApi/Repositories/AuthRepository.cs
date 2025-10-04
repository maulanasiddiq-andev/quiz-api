using AutoMapper;
using MailKit.Net.Smtp;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MimeKit;
using QuizApi.Constants;
using QuizApi.DTOs.Auth;
using QuizApi.DTOs.Identity;
using QuizApi.Exceptions;
using QuizApi.Extensions;
using QuizApi.Helpers;
using QuizApi.Models;
using QuizApi.Models.Auth;
using QuizApi.Models.Identity;
using QuizApi.Settings;

namespace QuizApi.Repositories
{
    public class AuthRepository
    {
        private readonly QuizAppDBContext dBContext;
        private readonly PasswordHasherHelper passwordHasherHelper;
        private readonly EmailSetting emailSetting;
        private readonly JWTSetting jwtSetting;
        private readonly IMapper mapper;
        private readonly string userId = "";
        public AuthRepository(
            QuizAppDBContext dBContext,
            IOptions<EmailSetting> emailOptions,
            IOptions<JWTSetting> jwtOptions,
            IHttpContextAccessor httpContextAccessor,
            IMapper mapper
        )
        {
            this.dBContext = dBContext;
            this.mapper = mapper;
            emailSetting = emailOptions.Value;
            jwtSetting = jwtOptions.Value;
            passwordHasherHelper = new PasswordHasherHelper();

            if (httpContextAccessor != null)
            {
                userId = httpContextAccessor.HttpContext?.GetUserId() ?? "";
            }
        }

        public async Task RegisterAsync(UserModel user, string password)
        {
            var userExists = await IsValidToCreateUser(user.Email, user.Username);

            if (!userExists)
            {
                throw new KnownException($"User dengan email {user.Email} atau username {user.Username} sudah ada");
            }

            user.UserId = Guid.NewGuid().ToString("N");
            user.HashedPassword = passwordHasherHelper.Hash(password);
            user.CreatedTime = DateTime.UtcNow;
            user.ModifiedTime = DateTime.UtcNow;
            user.RecordStatus = RecordStatusConstant.Active;

            await SendOTPEmailAsync(user);

            await dBContext.AddAsync(user);
            await dBContext.SaveChangesAsync();
        }

        public async Task SendOTPEmailAsync(UserModel user)
        {
            Random rnd = new Random();
            var otpCode = rnd.Next(1000, 9999);

            var otp = new OtpModel
            {
                OtpId = Guid.NewGuid().ToString("N"),
                Email = user.Email,
                OtpCode = otpCode,
                CreatedTime = DateTime.UtcNow,
                ModifiedTime = DateTime.UtcNow,
                RecordStatus = RecordStatusConstant.Active
            };

            await dBContext.AddAsync(otp);
            await dBContext.SaveChangesAsync();

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("MS Developer Video App", "maulanasiddiqdeveloper@gmail.com"));
            message.To.Add(new MailboxAddress(user.Name, user.Email));
            message.Subject = "Kode OTP";

            message.Body = new TextPart("plain")
            {
                Text = $"Kode OTP Anda adalah {otpCode}"
            };

            using (var client = new SmtpClient())
            {
                client.Connect(emailSetting.SmtpServer, emailSetting.Port, MailKit.Security.SecureSocketOptions.StartTls);
                client.Authenticate(emailSetting.Username, emailSetting.Password);

                client.Send(message);
                client.Disconnect(true);
            }
        }

        public async Task CheckOtpValidationAsync(CheckOtpDto checkOtpDto)
        {
            var otpExists = await dBContext.Otp.AnyAsync(x => x.Email == checkOtpDto.Email &&
                                                            x.OtpCode == checkOtpDto.OtpCode &&
                                                            x.RecordStatus.ToLower().Equals(RecordStatusConstant.Active.ToLower()) &&
                                                            DateTime.UtcNow < x.ExpiredTime);
            if (otpExists == false)
            {
                throw new KnownException("Kode OTP tidak valid");
            }

            var user = await dBContext.User.Where(x => x.Email == checkOtpDto.Email).FirstOrDefaultAsync();
            if (user == null)
            {
                throw new KnownException("User tidak ditemukan");
            }

            user.EmailVerifiedTime = DateTime.UtcNow;
            dBContext.Update(user);
            await dBContext.SaveChangesAsync();
        }

        public async Task<bool> IsValidToCreateUser(string email, string username)
        {
            bool user = await dBContext.User
                .AnyAsync(user =>
                    (user.Email == email || user.Username.ToLower() == username.ToLower()) &&
                    user.RecordStatus.ToLower() == RecordStatusConstant.Active.ToLower());

            return !user;
        }

        public async Task<UserModel?> FindUserByEmailAsync(string email)
        {
            return await dBContext.User.Where(x => x.Email == email).FirstOrDefaultAsync();
        }

        public async Task IncrementFailedLoginAttempts(UserModel user)
        {
            user.FailedLoginAttempts++;

            dBContext.Update(user);
            await dBContext.SaveChangesAsync();
        }

        public async Task<bool> IsLoginValidAsync(UserModel user, string password)
        {
            var isPasswordValid = passwordHasherHelper.IsPasswordValid(user.HashedPassword, password);
            if (!isPasswordValid && user.EmailVerifiedTime != null)
            {
                await IncrementFailedLoginAttempts(user);
            }

            return isPasswordValid && user.EmailVerifiedTime != null;
        }

        public async Task UpdateLastLoginTimeAsync(UserModel user)
        {
            user.LastLoginTime = DateTime.UtcNow;

            dBContext.Update(user);
            await dBContext.SaveChangesAsync();
        }

        public async Task<TokenDto> GenerateAndSaveLoginToken(UserModel user, string userAgent)
        {
            DateTime expiredTime = DateTime.UtcNow.AddHours(jwtSetting.TokenExpiredTimeInHour);
            var jwtToken = AuthorizationHelper.GenerateJWTToken(jwtSetting, expiredTime, user);

            var userToken = new UserTokenModel
            {
                UserTokenId = Guid.NewGuid().ToString("N"),
                UserId = user.UserId,
                IsAccessAllowed = true,
                ExpiredTime = expiredTime,
                Token = jwtToken,
                Browser = "",
                Device = "",
                OsVersion = "",
                Location = "",
                UserAgent = userAgent,
                RecordStatus = RecordStatusConstant.Active,
                CreatedBy = user.UserId,
                ModifiedBy = user.UserId,
                RefreshToken = AuthorizationHelper.GenerateRandomAlphaNumeric(),
                RefreshTokenExpiredTime = DateTime.UtcNow.AddHours(jwtSetting.RefreshTokenExpiredTimeInHour),
                CreatedTime = DateTime.UtcNow,
                ModifiedTime = DateTime.UtcNow,
                Description = ""
            };

            await dBContext.AddAsync(userToken);
            await dBContext.SaveChangesAsync();

            var tokenDto = new TokenDto
            {
                Token = jwtToken,
                IsValidLogin = true,
                RefreshToken = userToken.RefreshToken,
                RefreshTokenExpiredTime = userToken.RefreshTokenExpiredTime
            };

            return tokenDto;
        }

        public async Task<UserDto> CheckAuthAsync()
        {
            UserModel? user = await dBContext.User
                .Where(x => x.UserId.Equals(userId) && x.RecordStatus == RecordStatusConstant.Active)
                .FirstOrDefaultAsync();

            if (user is null)
            {
                throw new KnownException(ErrorMessageConstant.DataNotFound);
            }

            UserDto userDto = mapper.Map<UserDto>(user);

            return userDto;
        }
    }
}