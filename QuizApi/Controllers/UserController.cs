using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuizApi.Attributes;
using QuizApi.Constants;
using QuizApi.DTOs.Request;
using QuizApi.Extensions;
using QuizApi.Repositories;
using QuizApi.Responses;
using QuizApi.Services;

namespace QuizApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    [TokenValidation]
    public class UserController : ControllerBase
    {
        private readonly UserRepository userRepository;
        private readonly ActivityLogService activityLogService;
        public UserController(UserRepository userRepository, ActivityLogService activityLogService)
        {
            this.userRepository = userRepository;
            this.activityLogService = activityLogService;
        }

        [HttpGet]
        public async Task<BaseResponse> SearchCategoriesAsync([FromQuery] SearchRequestDto searchRequest)
        {
            try
            {
                var result = await userRepository.SearchDatasAsync(searchRequest);

                return new BaseResponse(true, "", result);
            }
            catch (Exception ex)
            {
                activityLogService.SaveErrorLog(ex, this.GetActionName(), this.GetUserId());

                return new BaseResponse(false, ErrorMessageConstant.ServerError, null);
            }
        }
    }
}