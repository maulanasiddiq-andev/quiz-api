using Microsoft.EntityFrameworkCore;
using QuizApi.Constants;
using QuizApi.DTOs.Auth;
using QuizApi.Exceptions;
using QuizApi.Models;
using QuizApi.Models.Auth;

namespace QuizApi.Services
{
    public class AuthorizationService
    {
        private readonly QuizAppDBContext _dBContext;
        private readonly CacheService _cacheService;
        public AuthorizationService(
            QuizAppDBContext dBContext,
            CacheService cacheService
        )
        {
            _dBContext = dBContext;
            _cacheService = cacheService;
        }

        public async Task<UserTokenDto?> ValidateTokenAsync(string userId, string? token)
        {
            try
            {
                if (token is null)
                {
                    throw new KnownException(ErrorMessageConstant.DataNotFound);
                }

                DateTime dateNow = DateTime.UtcNow;

                UserTokenModel? userTokenInMemory = _cacheService.GetData<UserTokenModel>(MemoryCacheConstant.UserTokenKey);
                UserTokenModel? userTokenLogin = null;

                if (userTokenInMemory is not null)
                {
                    if (
                        userTokenInMemory.RecordStatus.ToLower().Equals(RecordStatusConstant.Active.ToLower()) &&
                        userTokenInMemory.UserId.Equals(userId) &&
                        userTokenInMemory.Token.Equals(token)
                    )
                    {
                        userTokenLogin = userTokenInMemory;
                    }
                }

                if (
                    userTokenLogin is null ||
                    userTokenLogin.ExpiredTime < dateNow ||
                    userTokenLogin.IsAccessAllowed == false
                )
                {
                    UserTokenModel? userTokenInDB = await _dBContext.UserToken
                        .SingleOrDefaultAsync(x =>
                            x.RecordStatus.ToLower().Equals(RecordStatusConstant.Active.ToLower()) &&
                            x.UserId.Equals(userId) &&
                            x.Token.Equals(token)
                        );

                    if (userTokenInDB is not null)
                    {
                        userTokenLogin = userTokenInDB;

                        _cacheService.SetData(MemoryCacheConstant.UserTokenKey, userTokenLogin, TimeSpan.FromMinutes(50));
                    }
                }

                if (userTokenLogin is null)
                {
                    return null;
                }
                else if (userTokenLogin.ExpiredTime < dateNow)
                {
                    userTokenLogin.IsAccessAllowed = false;
                }

                if (!userTokenLogin.IsAccessAllowed)
                {
                    userTokenLogin.IsAccessAllowed = false;
                }

                UserTokenDto tokenDto = new UserTokenDto
                {
                    UserTokenId = userTokenLogin.UserTokenId,
                    UserId = userTokenLogin.UserId,
                    IsAccessAllowed = userTokenLogin.IsAccessAllowed,
                    ExpiredTime = userTokenLogin.ExpiredTime
                };

                return tokenDto;
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}