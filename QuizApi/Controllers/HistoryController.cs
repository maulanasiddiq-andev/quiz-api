using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuizApi.Attributes;
using QuizApi.Constants;
using QuizApi.Exceptions;
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
    public class HistoryController : ControllerBase
    {
        private readonly HistoryRepository historyRepository;
        private readonly ActivityLogService activityLogService;
        public HistoryController(HistoryRepository historyRepository, ActivityLogService activityLogService)
        {
            this.historyRepository = historyRepository;
            this.activityLogService = activityLogService;
        }

        [HttpGet]
        [Route("{id}")]
        public async Task<BaseResponse> GetQuizHistoryByIdAsync([FromRoute] string id)
        {
            try
            {
                var quizHistory = await historyRepository.GetDataByIdAsync(id);

                return new BaseResponse(true, "", quizHistory);
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
    }
}