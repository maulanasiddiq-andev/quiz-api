using System.Security.Claims;

namespace QuizApi.Extensions
{
    public static class HttpContextExtension
    {
        public static string? GetUserId(this HttpContext httpContext)
        {
            if (httpContext != null)
            {
                return httpContext.User.Claims.SingleOrDefault(a => a.Type == ClaimTypes.Name)?.Value;
            }

            return null;
        }
    }
}