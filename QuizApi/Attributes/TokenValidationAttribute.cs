using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Google.Cloud.Firestore;
using QuizApi.Models.Auth;
using QuizApi.Services;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;

namespace QuizApi.Attributes
{
    public class TokenValidationAttribute : Attribute, IAsyncAuthorizationFilter
    {
        public async Task OnAuthorizationAsync(AuthorizationFilterContext authorization)
        {
            if (authorization is null) throw new ArgumentNullException(nameof(authorization));

            var context = authorization.HttpContext;
            
            // Resolve the specific Keyed Firestore service
            var firestoreDb = context.RequestServices.GetRequiredKeyedService<FirestoreDb>("quiz-db");

            if (context.User.Identity is not null && context.User.Identity.IsAuthenticated)
            {
                // In JWT usually stored in "NameIdentifier" or "sub", 
                // but sticking to your ClaimTypes.Name per current code
                string? userId = context.User.FindFirst(ClaimTypes.Name)?.Value;
                var authHeader = context.Request.Headers["Authorization"].ToString();

                if (userId is null || string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
                {
                    SetUnauthorized(authorization);
                    return;
                }

                var token = authHeader.Substring("Bearer ".Length);
                var authService = new AuthorizationService(firestoreDb);
                
                var validateToken = await authService.ValidateTokenAsync(userId, token);

                if (validateToken is null || !validateToken.IsAccessAllowed)
                {
                    SetUnauthorized(authorization);
                    return;
                }
            }
        }

        private void SetUnauthorized(AuthorizationFilterContext context)
        {
            context.Result = new StatusCodeResult(StatusCodes.Status401Unauthorized);
        }
    }
}