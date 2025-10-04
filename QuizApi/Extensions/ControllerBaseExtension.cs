using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace QuizApi.Extensions
{
    public static class ControllerBaseExtension
    {
        public static string GetActionName(this ControllerBase controllerBase)
        {
            ControllerContext controllerContext = controllerBase.ControllerContext;

            return $"{controllerContext.ActionDescriptor.ActionName}_{controllerContext.ActionDescriptor.ControllerName}";
        }

        public static string GetUserId(this ControllerBase controllerBase)
        {
            ControllerContext controllerContext = controllerBase.ControllerContext;
            string userId = "";

            if (controllerBase != null && controllerBase.Request.HttpContext != null)
            {
                string? userIdFromRequest = controllerBase.Request.HttpContext.User.Claims.SingleOrDefault(a => a.Type == ClaimTypes.Name)?.Value;

                userId = userIdFromRequest ?? "NoLoginUser";
            }

            return userId;
        }
    }
}