using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuizApi.Attributes;
using QuizApi.Constants;
using QuizApi.DTOs.Identity;
using QuizApi.DTOs.Request;
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
    public class UserController : ControllerBase
    {
        private readonly UserRepository userRepository;
        private readonly ActivityLogService activityLogService;
        public UserController(UserRepository userRepository, ActivityLogService activityLogService)
        {
            this.userRepository = userRepository;
            this.activityLogService = activityLogService;
        }

        [RoleModuleValidation(ModuleConstant.SearchUser)]
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

        [RoleModuleValidation(ModuleConstant.DetailUser)]
        [HttpGet("{id}")]
        public async Task<BaseResponse> GetUserByIdAsync([FromRoute] string id)
        {
            try
            {
                if (id is null)
                {
                    throw new KnownException(ErrorMessageConstant.MethodParameterNull);
                }

                var user = await userRepository.GetDataByIdAsync(id);

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

        [RoleModuleValidation(ModuleConstant.EditUser)]
        [HttpPut]
        [Route("{id}")]
        public async Task<BaseResponse> UpdateCategoryByIdAsync([FromRoute] string id, [FromBody] UserDto user)
        {
            try
            {
                if (user is null || id is null)
                {
                    throw new KnownException(ErrorMessageConstant.MethodParameterNull);
                }

                var validator = new UserUpdateValidator();
                var results = validator.Validate(user);
                if (!results.IsValid)
                {
                    var messages = results.Errors.Select(x => x.ErrorMessage).ToList();
                    return new BaseResponse(false, messages);
                }

                await userRepository.UpdateDataAsync(id, user);

                return new BaseResponse(true, "User berhasil diupdate", null);
            }
            catch (KnownException ex)
            {
                activityLogService.SaveErrorLog(ex, this.GetActionName(), this.GetUserId());

                return new BaseResponse(false, ex.Message, null);
            }
            catch (DbUpdateConcurrencyException)
            {
                return new BaseResponse(false, ErrorMessageConstant.ItemAlreadyChanged, null);
            }
            catch (Exception ex)
            {
                activityLogService.SaveErrorLog(ex, this.GetActionName(), this.GetUserId());

                return new BaseResponse(false, ErrorMessageConstant.ServerError, null);
            }
        }

        [RoleModuleValidation(ModuleConstant.DeleteUser)]
        [HttpDelete]
        [Route("{id}")]
        public async Task<BaseResponse> DeleteCategoryByIdAsync([FromRoute] string id)
        {
            try
            {
                await userRepository.DeleteDataAsync(id);

                return new BaseResponse(true, "User berhasil dihapus", null);
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

        [RoleModuleValidation(ModuleConstant.SearchQuiz)]
        [HttpGet]
        [Route("{id}/quiz")]
        public async Task<BaseResponse> GetQuizzesByUserIdAsync([FromRoute] string id, [FromQuery] SearchRequestDto searchRequest)
        {
            try
            {
                var result = await userRepository.GetQuizzesByUserIdAsync(id, searchRequest);

                return new BaseResponse(true, "", result);
            }
            catch (Exception ex)
            {
                activityLogService.SaveErrorLog(ex, this.GetActionName(), this.GetUserId());

                return new BaseResponse(false, ErrorMessageConstant.ServerError, null);
            }
        }

        [RoleModuleValidation(ModuleConstant.DetailUser)]
        [HttpGet]
        [Route("{id}/history")]
        public async Task<BaseResponse> GetHistoriesByUserIdAsync([FromRoute] string id, [FromQuery] SearchRequestDto searchRequest)
        {
            try
            {
                var result = await userRepository.GetHistoriesByUserIdAsync(id, searchRequest);

                return new BaseResponse(true, "", result);
            }
            catch (Exception ex)
            {
                activityLogService.SaveErrorLog(ex, this.GetActionName(), this.GetUserId());

                return new BaseResponse(false, ErrorMessageConstant.ServerError, null);
            }
        }

        [HttpGet]
        [Route("self-quiz")]
        public async Task<BaseResponse> GetSelfQuizzesAsync([FromQuery] SearchRequestDto searchRequest)
        {
            try
            {
                var result = await userRepository.GetSelfQuizzesAsync(searchRequest);

                return new BaseResponse(true, "", result);
            }
            catch (Exception ex)
            {
                activityLogService.SaveErrorLog(ex, this.GetActionName(), this.GetUserId());

                return new BaseResponse(false, ErrorMessageConstant.ServerError, null);
            }
        }

        [HttpGet]
        [Route("self-history")]
        public async Task<BaseResponse> GetSelfHistoriesAsync([FromQuery] SearchRequestDto searchRequest)
        {
            try
            {
                var result = await userRepository.GetSelfHistoriesAsync(searchRequest);

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