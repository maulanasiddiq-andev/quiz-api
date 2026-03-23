using AutoMapper;
using Google.Apis.Auth;
using Google.Cloud.Firestore;
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
using QuizApi.Queue;
using QuizApi.Services;
using QuizApi.Settings;

namespace QuizApi.Repositories
{
    public class AuthRepository
    {
        private readonly FirestoreDb firestoreDb;
        private readonly PasswordHasherHelper passwordHasherHelper;
        private readonly JWTSetting jwtSetting;
        private readonly GoogleSetting googleSetting;
        private readonly IMapper mapper;
        private readonly RoleRepository roleRepository;
        private readonly UserRepository userRepository;
        private readonly string userId = "";
        private readonly ActionModelHelper actionModelHelper;
        private readonly EmailService emailService;
        public AuthRepository(
            [FromKeyedServices("quiz-db")] FirestoreDb firestoreDb,
            IOptions<JWTSetting> jwtOptions,
            IOptions<GoogleSetting> googleOptions,
            IHttpContextAccessor httpContextAccessor,
            IMapper mapper,
            RoleRepository roleRepository,
            UserRepository userRepository,
            EmailService emailService
        )
        {
            this.firestoreDb = firestoreDb;
            this.mapper = mapper;
            this.roleRepository = roleRepository;
            this.userRepository = userRepository;
            this.emailService = emailService;
            jwtSetting = jwtOptions.Value;
            googleSetting = googleOptions.Value;
            passwordHasherHelper = new PasswordHasherHelper();
            actionModelHelper = new ActionModelHelper();

            if (httpContextAccessor != null)
            {
                userId = httpContextAccessor.HttpContext?.GetUserId() ?? "";
            }
        }

        public async Task<UserDto> RegisterAsync(UserModel user, string password)
        {
            // check if the email submitted by the new user is already registered
            var isValid = await IsValidToCreateUser(user.Email);

            if (!isValid)
            {
                throw new KnownException($"User dengan email {user.Email} sudah ada");
            }

            // get the main role for being assigned to newly added user for default
            Query query = firestoreDb.Collection("role")
                .WhereEqualTo("IsMain", true)
                .Limit(1);
            QuerySnapshot snapshot = await query.GetSnapshotAsync();

            if (snapshot.Documents.Count > 0)
            {
                var role = snapshot.Documents[0].ConvertTo<RoleModel>();
                user.RoleId = role.RoleId;
            }

            actionModelHelper.AssignCreateModel(user, "User", "");
            user.HashedPassword = passwordHasherHelper.Hash(password);

            // username is taken from the first part of email (before @)
            user.Username = user.Email.Split('@')[0];

            await SendOTPEmailAsync(user);

            await firestoreDb.Collection("user").AddAsync(user);

            // return the user 
            // useful if the email is wrong and user wants to change it
            return mapper.Map<UserDto>(user);
        }

        public async Task<UserDto> ChangeEmailAsync(UserDto userDto)
        {
            Query query = firestoreDb.Collection("user")
                .WhereEqualTo("UserId", userDto.UserId)
                .WhereEqualTo("RecordStatus", RecordStatusConstant.Active)
                .Limit(1);
            QuerySnapshot snapshot = await query.GetSnapshotAsync();

            if (snapshot.Documents.Count == 0)
            {
                throw new KnownException(ErrorMessageConstant.DataNotFound);
            }

            DocumentSnapshot targetDoc = snapshot.Documents[0];
            UserModel user = targetDoc.ConvertTo<UserModel>();

            // check if the email already exists
            var isValid = await IsValidToCreateUser(userDto.Email);
            if (!isValid)
            {
                throw new KnownException("Email sudah dipakai");
            }

            WriteBatch batch = firestoreDb.StartBatch();

            // change the email and username, save, then send a new otp to the new email
            user.Email = userDto.Email;
            user.Username = userDto.Email.Split("@")[0];
            
            batch.Set(targetDoc.Reference, user);
            await batch.CommitAsync();

            await SendOTPEmailAsync(user);

            return mapper.Map<UserDto>(user);
        }

        public async Task ResendOTPEmailAsync(UserModel user)
        {
            Query query = firestoreDb.Collection("otp")
                .WhereEqualTo("Email", user.Email)
                .WhereEqualTo("RecordStatus", RecordStatusConstant.Active)
                .OrderByDescending("CreatedTime");
            QuerySnapshot snapshot = await query.GetSnapshotAsync();

            if (snapshot.Documents.Count > 0)
            {
                List<OtpModel> otps = snapshot.Documents.Select(doc => doc.ConvertTo<OtpModel>()).ToList();
                // check if the previous otp surpasses 10 minutes
                // the first otps is the exact previous otp
                TimeSpan timeSpan = DateTime.UtcNow - (otps[0].CreatedTime ?? DateTime.UtcNow);
                if (timeSpan.TotalMinutes < 10)
                {
                    throw new KnownException("Kode OTP baru hanya bisa diminta setelah 10 menit");
                }
                else
                {
                    await SendOTPEmailAsync(user);
                }
            }
            else
            {
                // if the otp for the newly registered user is not found
                // send
                await SendOTPEmailAsync(user);
            }
        }

        private async Task SendOTPEmailAsync(UserModel user)
        {
            Random rnd = new Random();
            var otpCode = rnd.Next(1000, 9999);

            var otp = new OtpModel
            {
                Email = user.Email,
                OtpCode = otpCode,
            };

            actionModelHelper.AssignCreateModel(otp, "Otp", user.UserId);

            await firestoreDb.Collection("otp").AddAsync(otp);

            await emailService.SendEmailAsync(user.Name, user.Email, $"Kode OTP Anda adalah {otpCode}");
        }

        public async Task CheckOtpValidationAsync(CheckOtpDto checkOtpDto)
        {
            Timestamp now = Timestamp.FromDateTime(DateTime.UtcNow);

            Query query = firestoreDb.Collection("otp")
                .WhereEqualTo("Email", checkOtpDto.Email)
                .WhereEqualTo("OtpCode", checkOtpDto.OtpCode)
                .WhereEqualTo("RecordStatus", RecordStatusConstant.Active)
                .WhereGreaterThan("ExpiredTime", now);

            QuerySnapshot snapshot = await query.GetSnapshotAsync();

            if (snapshot.Documents.Count == 0)
            {
                throw new KnownException("Kode OTP tidak valid");
            }

            Query userQuery = firestoreDb.Collection("user")
                .WhereEqualTo("Email", checkOtpDto.Email)
                .Limit(1);
            QuerySnapshot userSnapshot = await userQuery.GetSnapshotAsync();
        
            if (userSnapshot.Documents.Count == 0)
            {
                throw new KnownException("User tidak ditemukan");
            }

            WriteBatch batch = firestoreDb.StartBatch();

            DocumentSnapshot targetDoc = userSnapshot.Documents[0];
            UserModel user = targetDoc.ConvertTo<UserModel>();

            user.EmailVerifiedTime = DateTime.UtcNow;

            batch.Set(targetDoc.Reference, user);
            await batch.CommitAsync();
        }

        public async Task<bool> IsValidToCreateUser(string email)
        {
            Query query = firestoreDb.Collection("user")
                .WhereEqualTo("Email", email)
                .WhereEqualTo("RecordStatus", RecordStatusConstant.Active)
                .Limit(1);
            QuerySnapshot snapshot = await query.GetSnapshotAsync();

            // return true if no active user with the email is found, meaning it's valid to create new user with the email
            return snapshot.Documents.Count == 0;
        }

        public async Task<UserModel?> FindUserByEmailAsync(string email)
        {
            Query query = firestoreDb.Collection("user").WhereEqualTo("Email", email).Limit(1);

            QuerySnapshot snapshot = await query.GetSnapshotAsync();

            if (snapshot.Documents.Count > 0)
            {
                return snapshot.Documents[0].ConvertTo<UserModel>();
            }

            return null;
        }

        public async Task IncrementFailedLoginAttempts(UserModel user)
        {
            Query query = firestoreDb.Collection("user")
                .WhereEqualTo("UserId", user.UserId)
                .Limit(1);
            QuerySnapshot snapshot = await query.GetSnapshotAsync();

            if (snapshot.Documents.Count == 0)
            {
                throw new KnownException(ErrorMessageConstant.DataNotFound);
            }

            user.FailedLoginAttempts++;

            await snapshot.Documents[0].Reference.UpdateAsync("FailedLoginAttempts", user.FailedLoginAttempts);
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
            // Find the document
            Query query = firestoreDb.Collection("user").WhereEqualTo("UserId", user.UserId).Limit(1);
            QuerySnapshot snapshot = await query.GetSnapshotAsync();
            DocumentSnapshot? document = snapshot.Documents.FirstOrDefault();

            if (document != null && document.Exists)
            {
                // Update using the reference found in the snapshot
                await document.Reference.UpdateAsync("LastLoginTime", Timestamp.FromDateTime(DateTime.UtcNow));
            }
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
                role!.RoleModules = mapper.Map<List<RoleModuleDto>>(roleModules);

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

            await firestoreDb.Collection("usertoken").AddAsync(userToken);

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
            Query query = firestoreDb.Collection("usertoken").WhereEqualTo("RefreshToken", tokenDto.RefreshToken).Limit(1);
            QuerySnapshot snapshot = await query.GetSnapshotAsync();

            if (snapshot.Documents.Count == 0)
            {
                throw new KnownException(ErrorMessageConstant.DataNotFound);
            }

            return snapshot.Documents[0].ConvertTo<UserTokenModel>();
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
            Query query = firestoreDb.Collection("user")
                .WhereEqualTo("UserId", userId)
                .WhereEqualTo("RecordStatus", RecordStatusConstant.Active)
                .Limit(1);
            QuerySnapshot snapshot = await query.GetSnapshotAsync();

            if (snapshot.Documents.Count == 0)
            {
                throw new KnownException(ErrorMessageConstant.DataNotFound);
            }

            UserModel user = snapshot.Documents[0].ConvertTo<UserModel>();
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
            Query query = firestoreDb.Collection("role")
                .WhereEqualTo("IsMain", true)
                .Limit(1);
            QuerySnapshot snapshot = await query.GetSnapshotAsync();

            if (snapshot.Documents.Count > 0)
            {
                RoleModel role = snapshot.Documents[0].ConvertTo<RoleModel>();
                newUser.RoleId = role.RoleId;
            }

            await firestoreDb.Collection("user").AddAsync(newUser);

            return newUser;
        }

        // create fcm token when the user is logged in
        public async Task CreateFcmTokenAsync(string fcmToken, string? device, string userId)
        {
            var collectionRef = firestoreDb.Collection("fcmtoken");

            // 1. Check if fcm token is already registered for this user
            Query query = collectionRef
                .WhereEqualTo("Token", fcmToken)
                .WhereEqualTo("UserId", userId)
                .Limit(1);

            QuerySnapshot snapshot = await query.GetSnapshotAsync();
            DocumentSnapshot? existingDoc = snapshot.Documents.FirstOrDefault();

            if (existingDoc != null && existingDoc.Exists)
            {
                // 2. If it exists, update to active
                var storedFcmToken = existingDoc.ConvertTo<FcmTokenModel>();
                storedFcmToken.RecordStatus = RecordStatusConstant.Active;
                
                // Assuming your helper sets ModifiedTime and ModifiedBy
                actionModelHelper.AssignUpdateModel(storedFcmToken, userId);

                // Update the existing document using its unique ID
                await existingDoc.Reference.SetAsync(storedFcmToken, SetOptions.MergeAll);
                return;
            }

            // 3. Create a new one if not found
            var newToken = new FcmTokenModel
            {
                Token = fcmToken,
                Device = device ?? "",
                UserId = userId
            };

            actionModelHelper.AssignCreateModel(newToken, "FcmToken", userId);

            // AddAsync creates a new document with an auto-generated ID
            await firestoreDb.Collection("fcmtoken").AddAsync(newToken);
        }
        
        public async Task LogoutAsync(LogoutDto logoutDto)
        {
            // remove fcm token related to the device logged out
            Query query = firestoreDb.Collection("fcmtoken")
                .WhereEqualTo("UserId", userId)
                .WhereEqualTo("Token", logoutDto.FcmToken)
                .WhereEqualTo("RecordStatus", RecordStatusConstant.Active)
                .Limit(1);
            QuerySnapshot snapshot = await query.GetSnapshotAsync();
            
            if (snapshot.Documents.Count > 0)
            {
                WriteBatch batch = firestoreDb.StartBatch();

                DocumentSnapshot targetDoc = snapshot.Documents[0];
                FcmTokenModel fcmToken = targetDoc.ConvertTo<FcmTokenModel>();
                actionModelHelper.AssignDeleteModel(fcmToken, userId);

                batch.Set(targetDoc.Reference, fcmToken);
                await batch.CommitAsync();
            }
        }
    }
}