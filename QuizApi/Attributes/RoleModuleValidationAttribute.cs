using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Google.Cloud.Firestore;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using QuizApi.Services;

namespace QuizApi.Attributes
{
    public class RoleModuleValidationAttribute : Attribute, IAsyncAuthorizationFilter
    {
        private readonly List<string> _modules;

        public RoleModuleValidationAttribute(params string[] moduleNames)
        {
            _modules = moduleNames.ToList();
        }

        public async Task OnAuthorizationAsync(AuthorizationFilterContext authorizationFilterContext)
        {
            if (authorizationFilterContext is null) throw new ArgumentNullException(nameof(authorizationFilterContext));

            var context = authorizationFilterContext.HttpContext;
            
            // Resolve services
            var firestoreDb = context.RequestServices.GetRequiredKeyedService<FirestoreDb>("quiz-db");
            var logService = context.RequestServices.GetService<ActivityLogService>();

            if (context.User.Identity != null && context.User.Identity.IsAuthenticated)
            {
                string? userId = context.User.FindFirst(ClaimTypes.Name)?.Value;

                if (string.IsNullOrEmpty(userId))
                {
                    await SetForbidden(authorizationFilterContext);
                    return;
                }

                var validationService = new RoleModuleValidationService(firestoreDb, logService!);

                bool isAllowed = false;
                foreach (var module in _modules)
                {
                    if (await validationService.IsAllowAccessModuleAsync(userId, module))
                    {
                        isAllowed = true;
                        break;
                    }
                }

                if (!isAllowed)
                {
                    await SetForbidden(authorizationFilterContext);
                }
            }
        }

        private async Task SetForbidden(AuthorizationFilterContext context)
        {
            await context.HttpContext.ForbidAsync();
            context.Result = new StatusCodeResult(StatusCodes.Status403Forbidden);
        }
    }
}