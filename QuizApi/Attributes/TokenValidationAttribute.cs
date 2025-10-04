using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using QuizApi.Models;
using QuizApi.Services;

namespace QuizApi.Attributes
{
    public class TokenValidationAttribute : Attribute, IAsyncAuthorizationFilter
    {
        private CacheService? cacheService;
        public async Task OnAuthorizationAsync(AuthorizationFilterContext authorization)
        {
            if (authorization is null)
            {
                throw new ArgumentNullException(nameof(authorization));
            }

            var context = authorization.HttpContext;
            cacheService = context.RequestServices.GetService<CacheService>();
            var dbContext = context.RequestServices.GetService<QuizAppDBContext>();

            if (context.User.Identity is not null && context.User.Identity.IsAuthenticated)
            {
                string? userId = context.User.FindFirst(ClaimTypes.Name)?.Value;

                if (userId is null)
                {
                    await context.ChallengeAsync();
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    authorization.Result = new StatusCodeResult(StatusCodes.Status401Unauthorized);
                    return;
                }

                var authService = new AuthorizationService(dbContext!, cacheService!);

                if (
                    context.Request.Headers.ContainsKey("Authorization") &&
                    context.Request.Headers["Authorization"][0] is not null &&
                    context.Request.Headers["Authorization"][0]!.StartsWith("Bearer ")
                )
                {
                    var token = context.Request.Headers["Authorization"][0]?.Substring("Bearer ".Length);

                    var validateToken = await authService.ValidateTokenAsync(userId, token);

                    if (validateToken is null)
                    {
                        await context.ChallengeAsync();
                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        authorization.Result = new StatusCodeResult(StatusCodes.Status401Unauthorized);
                        return;
                    }

                    if (validateToken.IsAccessAllowed == false)
                    {
                        await context.ChallengeAsync();
                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        authorization.Result = new StatusCodeResult(StatusCodes.Status401Unauthorized);
                        return;
                    }
                }
            }
        }
    }
}