using AutoMapper;
using Google.Apis.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using QuizApi.Constants;
using QuizApi.DTOs.Auth;
using QuizApi.DTOs.Identity;
using QuizApi.Exceptions;
using QuizApi.Extensions;
using QuizApi.Helpers;
using QuizApi.Models;
using QuizApi.Models.Auth;
using QuizApi.Models.Identity;
using QuizApi.Services;
using QuizApi.Settings;

namespace QuizApi.Repositories
{
    public class AuthRepository
    {
        private readonly QuizAppDBContext dBContext;
        private readonly PasswordHasherHelper passwordHasherHelper;
        private readonly JWTSetting jwtSetting;
        private readonly GoogleSetting googleSetting;
        private readonly IMapper mapper;
        private readonly RoleRepository roleRepository;
        private readonly UserRepository userRepository;
        private readonly EmailService emailService;
        private readonly string userId = "";
        public AuthRepository(
            QuizAppDBContext dBContext,
            IOptions<JWTSetting> jwtOptions,
            IOptions<GoogleSetting> googleOptions,
            IHttpContextAccessor httpContextAccessor,
            IMapper mapper,
            RoleRepository roleRepository,
            EmailService emailService,
            UserRepository userRepository
        )
        {
            this.dBContext = dBContext;
            this.mapper = mapper;
            this.roleRepository = roleRepository;
            this.emailService = emailService;
            this.userRepository = userRepository;
            jwtSetting = jwtOptions.Value;
            googleSetting = googleOptions.Value;
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

            // get the main role for being assigned to newly added user for default
            RoleModel? role = await dBContext.Role.Where(x => x.IsMain == true).FirstOrDefaultAsync();

            if (role != null)
            {
                user.RoleId = role.RoleId;
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

            await emailService.SendEmailAsync(
                user.Name,
                user.Email,
                $"Kode OTP Anda adalah {otpCode}"
            );
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
            RoleDto? role = null;
            List<string> roleModuleNames = new();

            if (user.RoleId != null)
            {
                role = await roleRepository.GetDataByIdAsync(user.RoleId);
                var roleModules = await roleRepository.GetRoleModulesByRoleId(user.RoleId);

                // assign role modules for checking features access permission on UI
                role.RoleModules = mapper.Map<List<RoleModuleDto>>(roleModules);

                // take only names, for assigning them to JWT and checking permission on every request
                roleModuleNames = roleModules.Select(x => x.RoleModuleName).Order().ToList();
            }

            DateTime expiredTime = DateTime.UtcNow.AddHours(jwtSetting.TokenExpiredTimeInHour);
            var jwtToken = AuthorizationHelper.GenerateJWTToken(jwtSetting, expiredTime, user, role?.Name, roleModuleNames);

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
                RefreshTokenExpiredTime = userToken.RefreshTokenExpiredTime,
                User = mapper.Map<UserDto>(user)
            };

            tokenDto.User.Role = role;

            return tokenDto;
        }

        public async Task<UserTokenModel> GetUserTokenModelByRefreshTokenAsync(TokenDto tokenDto)
        {
            UserTokenModel? userToken = await dBContext.UserToken
                .Where(x => x.RefreshToken == tokenDto.RefreshToken)
                .FirstOrDefaultAsync();

            if (userToken == null)
            {
                throw new KnownException(ErrorMessageConstant.DataNotFound);
            }

            return userToken;
        }
        
        public async Task<TokenDto> RefreshTokenAsync(TokenDto tokenDto, string userAgent)
        {
            // find old token
            UserTokenModel userToken = await GetUserTokenModelByRefreshTokenAsync(tokenDto);

            if (userToken.IsAccessAllowed == false)
            {
                throw new KnownException(ErrorMessageConstant.InvalidLogin);
            }

            // find user
            UserDto? userDto = await userRepository.GetDataByIdAsync(userToken.UserId);

            if (userDto == null)
            {
                throw new KnownException(ErrorMessageConstant.InvalidLogin);
            }

            UserModel user = mapper.Map<UserModel>(userDto);

            TokenDto token = await GenerateAndSaveLoginToken(user, userAgent);

            return token;
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

        public async Task<UserModel> LoginWithGoogleAsync(LoginWithGoogleDto loginWithGoogleDto)
        {
            var payload = await GoogleJsonWebSignature.ValidateAsync(loginWithGoogleDto.IdToken, new GoogleJsonWebSignature.ValidationSettings
            {
                Audience = new[] { googleSetting.ServerClientId } // <-- From Google Console
            });

            var user = await FindUserByEmailAsync(payload.Email);

            // if user already exists, return user immediately
            if (user != null)
            {
                return user;
            }

            // if user doesn't exist yet, create new user
            var newUser = new UserModel
            {
                UserId = Guid.NewGuid().ToString("N"),
                Name = payload.Name,
                // username is taken from the part before @
                Username = payload.Email.Split("@")[0],
                Email = payload.Email,
                CreatedTime = DateTime.UtcNow,
                ModifiedTime = DateTime.UtcNow,
                // immediately verify email
                EmailVerifiedTime = DateTime.UtcNow,
                RecordStatus = RecordStatusConstant.Active,
                ProfileImage = payload.Picture
            };

             // get the main role for being assigned to newly added user for default
            RoleModel? role = await dBContext.Role.Where(x => x.IsMain == true).FirstOrDefaultAsync();

            if (role != null)
            {
                newUser.RoleId = role.RoleId;
            }

            await dBContext.AddAsync(newUser);
            await dBContext.SaveChangesAsync();

            return newUser;
        }
    }
}