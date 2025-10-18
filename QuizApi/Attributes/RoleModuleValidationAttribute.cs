using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using QuizApi.Models;
using QuizApi.Services;

namespace QuizApi.Attributes
{
    public class RoleModuleValidationAttribute : Attribute, IAsyncAuthorizationFilter
    {
        private readonly List<string> modules;
        private CacheService? cacheService;

        public RoleModuleValidationAttribute(string moduleName)
        {
            modules = new List<string>() { moduleName };
        }

        public RoleModuleValidationAttribute(string moduleName1, string moduleName2)
        {
            modules = new List<string> { moduleName1, moduleName2 };
        }

        public RoleModuleValidationAttribute(string moduleName1, string moduleName2, string moduleName3)
        {
            modules = new List<string> { moduleName1, moduleName2, moduleName3 };
        }

        public RoleModuleValidationAttribute(string moduleName1, string moduleName2, string moduleName3, string moduleName4)
        {
            modules = new List<string> { moduleName1, moduleName2, moduleName3, moduleName4 };
        }

        public async Task OnAuthorizationAsync(AuthorizationFilterContext authorizationFilterContext)
        {
            if (authorizationFilterContext is null)
            {
                throw new ArgumentNullException(nameof(authorizationFilterContext));
            }

            var context = authorizationFilterContext.HttpContext;
            var dbContext = context.RequestServices.GetService<QuizAppDBContext>();
            var logService = context.RequestServices.GetService<ActivityLogService>();
            cacheService = context.RequestServices.GetService<CacheService>();

            if (context.User.Identity != null && context.User.Identity.IsAuthenticated)
            {
                string? userId = context.User.FindFirst(ClaimTypes.Name)?.Value;

                if (userId is null)
                {
                    await context.ForbidAsync();
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    authorizationFilterContext.Result = new StatusCodeResult(StatusCodes.Status403Forbidden);
                    return;
                }

                RoleModuleValidationService roleModulValidationService = new RoleModuleValidationService(dbContext!, logService!, cacheService!);

                bool isAllowAccess = false;

                if (modules != null)
                {
                    foreach (var module in modules)
                    {
                        var canAccess = await roleModulValidationService.IsAllowAccessModuleAsync(userId, module);

                        if (canAccess)
                        {
                            isAllowAccess = canAccess;
                            break;
                        }
                    }
                }

                if (isAllowAccess == false)
                {
                    await context.ForbidAsync();
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    authorizationFilterContext.Result = new StatusCodeResult(StatusCodes.Status403Forbidden);
                    return;
                }
            }
        }
    }
}