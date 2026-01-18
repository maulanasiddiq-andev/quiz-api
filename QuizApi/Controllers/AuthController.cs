using AutoMapper;
using Google.Apis.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuizApi.Attributes;
using QuizApi.Constants;
using QuizApi.DTOs.Auth;
using QuizApi.DTOs.Identity;
using QuizApi.Exceptions;
using QuizApi.Extensions;
using QuizApi.Models.Auth;
using QuizApi.Models.Identity;
using QuizApi.Repositories;
using QuizApi.Responses;
using QuizApi.Services;

namespace QuizApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IMapper mapper;
        private readonly AuthRepository authRepository;
        private readonly ActivityLogService activityLogService;
        public AuthController(
            IMapper mapper,
            AuthRepository authRepository,
            ActivityLogService activityLogService
        )
        {
            this.mapper = mapper;
            this.authRepository = authRepository;
            this.activityLogService = activityLogService;
        }

        [HttpPost]
        [Route("register")]
        public async Task<BaseResponse> RegisterAsync([FromBody] RegisterDto registerDto)
        {
            try
            {
                if (registerDto is null)
                {
                    throw new KnownException(ErrorMessageConstant.MethodParameterNull);
                }

                var validator = new RegisterValidator();
                var results = validator.Validate(registerDto);
                if (!results.IsValid)
                {
                    var messages = results.Errors.Select(error => error.ErrorMessage).ToList();
                    return new BaseResponse(false, messages);
                }

                var user = mapper.Map<UserModel>(registerDto);

                var result = await authRepository.RegisterAsync(user, registerDto.Password);

                return new BaseResponse(true, "Pendaftaran Berhasil", result);
            }
            catch (KnownException ex)
            {
                activityLogService.SaveErrorLog(ex, this.GetActionName(), this.GetUserId());

                return new BaseResponse(false, ex.Message, null);
            }
            catch (Exception ex)
            {
                activityLogService.SaveErrorLog(ex, this.GetActionName(), this.GetUserId());

                return new BaseResponse(false, ErrorMessageConstant.ServerError, null);
            }
        }

        [HttpPost]
        [Route("change-email")]
        public async Task<BaseResponse> ChangeEmailAsync([FromBody] UserDto userDto)
        {
            try
            {
                if (userDto is null)
                {
                    throw new KnownException(ErrorMessageConstant.MethodParameterNull);
                }

                var validator = new UserChangeEmailValidator();
                var results = validator.Validate(userDto);
                if (!results.IsValid)
                {
                    var messages = results.Errors.Select(error => error.ErrorMessage).ToList();
                    return new BaseResponse(false, messages);
                }

                var result = await authRepository.ChangeEmailAsync(userDto);

                return new BaseResponse(true, "Email berhasil diupdate", result);
            }
            catch (KnownException ex)
            {
                activityLogService.SaveErrorLog(ex, this.GetActionName(), this.GetUserId());

                return new BaseResponse(false, ex.Message, null);
            }
            catch (Exception ex)
            {
                activityLogService.SaveErrorLog(ex, this.GetActionName(), this.GetUserId());

                return new BaseResponse(false, ErrorMessageConstant.ServerError, null);
            }
        }

        [HttpPost]
        [Route("otp")]
        public async Task<BaseResponse> CheckOtpValidationAsync([FromBody] CheckOtpDto checkOtpDto)
        {
            try
            {
                if (checkOtpDto is null)
                {
                    throw new KnownException(ErrorMessageConstant.MethodParameterNull);
                }

                await authRepository.CheckOtpValidationAsync(checkOtpDto);

                return new BaseResponse(true, "Kode OTP berhasil di verifikasi, silakan login", null);
            }
            catch (KnownException ex)
            {
                activityLogService.SaveErrorLog(ex, this.GetActionName(), this.GetUserId());

                return new BaseResponse(false, ex.Message, null);
            }
            catch (Exception ex)
            {
                activityLogService.SaveErrorLog(ex, this.GetActionName(), this.GetUserId());

                return new BaseResponse(false, ErrorMessageConstant.ServerError, null);
            }
        }

        [HttpPost]
        [Route("resend-otp")]
        public async Task<BaseResponse> ResendOTPAsync([FromBody] UserDto userDto)
        {
            try
            {
                UserModel user = mapper.Map<UserModel>(userDto);
                await authRepository.ResendOTPEmailAsync(user);

                return new BaseResponse(true, "OTP baru sudah dikirim", null);
            }
            catch (KnownException ex)
            {
                activityLogService.SaveErrorLog(ex, this.GetActionName(), this.GetUserId());

                return new BaseResponse(false, ex.Message, null);
            }
            catch (Exception ex)
            {
                activityLogService.SaveErrorLog(ex, this.GetActionName(), this.GetUserId());

                return new BaseResponse(false, ErrorMessageConstant.ServerError, null);
            }
        }

        [HttpPost]
        [Route("login")]
        public async Task<BaseResponse> LoginAsync([FromBody] LoginDto loginDto)
        {
            try
            {
                if (loginDto is null)
                {
                    throw new KnownException(ErrorMessageConstant.MethodParameterNull);
                }

                var validator = new LoginValidator();
                var results = validator.Validate(loginDto);
                if (!results.IsValid)
                {
                    var messages = results.Errors.Select(x => x.ErrorMessage).ToList();
                    return new BaseResponse(false, messages);
                }

                var user = await authRepository.FindUserByEmailAsync(loginDto.Email);
                if (user is null)
                {
                    throw new KnownException(ErrorMessageConstant.InvalidLogin);
                }

                var isLoginValid = await authRepository.IsLoginValidAsync(user, loginDto.Password);
                if (!isLoginValid)
                {
                    throw new KnownException(ErrorMessageConstant.InvalidLogin);
                }

                var userAgent = string.IsNullOrEmpty(Request.Headers["User-Agent"]) ? "" : Request.Headers["User-Agent"].ToString();

                TokenDto tokenDto = await authRepository.GenerateAndSaveLoginToken(user, userAgent);
                await authRepository.UpdateLastLoginTimeAsync(user);

                // create fcm token for push notification
                if (!string.IsNullOrWhiteSpace(loginDto.FcmToken))
                {
                    await authRepository.CreateFcmTokenAsync(loginDto.FcmToken, loginDto.Device, user.UserId);
                }

                return new BaseResponse(true, "Login Berhasil", tokenDto);
            }
            catch (KnownException ex)
            {
                activityLogService.SaveErrorLog(ex, this.GetActionName(), this.GetUserId());

                return new BaseResponse(false, ex.Message, null);
            }
            catch (Exception ex)
            {
                activityLogService.SaveErrorLog(ex, this.GetActionName(), this.GetUserId());

                return new BaseResponse(false, ErrorMessageConstant.ServerError, null);
            }
        }

        [HttpPost]
        [Route("refresh-token")]
        public async Task<BaseResponse> RefreshTokenAsync([FromBody] TokenDto tokenDto)
        {
            try
            {
                if (tokenDto == null || string.IsNullOrEmpty(tokenDto.RefreshToken) || string.IsNullOrEmpty(tokenDto.Token))
                {
                    throw new KnownException(ErrorMessageConstant.MethodParameterNull);
                }

                var userAgent = string.IsNullOrEmpty(Request.Headers["User-Agent"]) ? "" : Request.Headers["User-Agent"].ToString();
                TokenDto token = await authRepository.RefreshTokenAsync(tokenDto, userAgent);

                return new BaseResponse(true, "Login Berhasil", token);
            }
            catch (KnownException ex)
            {
                activityLogService.SaveErrorLog(ex, this.GetActionName(), this.GetUserId());

                return new BaseResponse(false, ex.Message, null);
            }
            catch (Exception ex)
            {
                activityLogService.SaveErrorLog(ex, this.GetActionName(), this.GetUserId());

                return new BaseResponse(false, ErrorMessageConstant.ServerError, null);
            }
        }

        [HttpPost]
        [Route("login-with-google")]
        public async Task<BaseResponse> LoginWithGoogleAsync([FromBody] LoginWithGoogleDto loginWithGoogleDto)
        {
            try
            {
                var user = await authRepository.LoginWithGoogleAsync(loginWithGoogleDto);

                var userAgent = string.IsNullOrEmpty(Request.Headers["User-Agent"]) ? "" : Request.Headers["User-Agent"].ToString();

                TokenDto tokenDto = await authRepository.GenerateAndSaveLoginToken(user, userAgent);

                await authRepository.UpdateLastLoginTimeAsync(user);

                // create fcm token for push notification
                if (!string.IsNullOrWhiteSpace(loginWithGoogleDto.FcmToken))
                {
                    await authRepository.CreateFcmTokenAsync(loginWithGoogleDto.FcmToken, loginWithGoogleDto.Device, user.UserId);
                }
                
                return new BaseResponse(true, "", tokenDto);
            }
            catch (InvalidJwtException ex)
            {
                activityLogService.SaveErrorLog(ex, this.GetActionName(), this.GetUserId());

                return new BaseResponse(false, ErrorMessageConstant.ServerError, null);
            }
            catch (Exception ex)
            {
                activityLogService.SaveErrorLog(ex, this.GetActionName(), this.GetUserId());

                return new BaseResponse(false, ErrorMessageConstant.ServerError, null);
            }
        }

        [HttpGet]
        [Route("check-auth")]
        [Authorize]
        [TokenValidation]
        public async Task<BaseResponse> CheckAuthAsync()
        {
            try
            {
                var user = await authRepository.CheckAuthAsync();

                return new BaseResponse(true, "", user);
            }
            catch (KnownException ex)
            {
                activityLogService.SaveErrorLog(ex, this.GetActionName(), this.GetUserId());

                return new BaseResponse(false, ex.Message, null);
            }
            catch (Exception ex)
            {
                activityLogService.SaveErrorLog(ex, this.GetActionName(), this.GetUserId());

                return new BaseResponse(false, ErrorMessageConstant.ServerError, null);
            }
        }

        [HttpPost]
        [Route("logout")]
        [Authorize]
        [TokenValidation]
        public async Task<BaseResponse> LogoutAsync([FromBody] LogoutDto? logout)
        {
            try
            {
                if (logout != null)
                {
                    await authRepository.LogoutAsync(logout);
                }

                return new BaseResponse(true, "Berhasil Logout", null);
            }
            catch (Exception ex)
            {
                activityLogService.SaveErrorLog(ex, this.GetActionName(), this.GetUserId());

                return new BaseResponse(false, ErrorMessageConstant.ServerError, null);
            }
        }
    }
}