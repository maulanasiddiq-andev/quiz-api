using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuizApi.Attributes;
using QuizApi.Constants;
using QuizApi.DTOs.Quiz;
using QuizApi.DTOs.CheckQuiz;
using QuizApi.DTOs.Request;
using QuizApi.Exceptions;
using QuizApi.Extensions;
using QuizApi.Repositories;
using QuizApi.Responses;
using QuizApi.Services;
using Microsoft.EntityFrameworkCore;

namespace QuizApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    [TokenValidation]
    public class QuizController : ControllerBase
    {
        private readonly QuizRepository quizRepository;
        private readonly ActivityLogService activityLogService;
        public QuizController(
            QuizRepository quizRepository,
            ActivityLogService activityLogService
        )
        {
            this.quizRepository = quizRepository;
            this.activityLogService = activityLogService;
        }

        [RoleModuleValidation(ModuleConstant.SearchQuiz)]
        [HttpGet]
        public async Task<BaseResponse> SearchQuizzesAsync([FromQuery] QuizFilterDto searchRequest)
        {
            try
            {
                var result = await quizRepository.SearchDatasAsync(searchRequest);

                return new BaseResponse(true, "", result);
            }
            catch (Exception ex)
            {
                activityLogService.SaveErrorLog(ex, this.GetActionName(), this.GetUserId());

                return new BaseResponse(false, ErrorMessageConstant.ServerError, null);
            }
        }

        [RoleModuleValidation(ModuleConstant.CreateQuiz)]
        [HttpPost]
        public async Task<BaseResponse> CreateQuizAsync([FromBody] QuizDto quizDto)
        {
            try
            {
                if (quizDto == null)
                {
                    throw new KnownException(ErrorMessageConstant.MethodParameterNull);
                }

                var validator = new QuizValidator();
                var results = validator.Validate(quizDto);
                if (!results.IsValid)
                {
                    var messages = results.Errors.Select(x => x.ErrorMessage).ToList();
                    return new BaseResponse(false, messages);
                }

                await quizRepository.CreateDataAsync(quizDto);

                return new BaseResponse(true, "Kuis berhasil ditambahkan", null);
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

        [RoleModuleValidation(ModuleConstant.DetailQuiz)]
        [HttpGet("{id}")]
        public async Task<BaseResponse> GetQuizByIdAsync([FromRoute] string id)
        {
            try
            {
                if (id is null)
                {
                    throw new KnownException(ErrorMessageConstant.MethodParameterNull);
                }

                var category = await quizRepository.GetDataByIdAsync(id);

                return new BaseResponse(true, "", category);
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

        // for updating
        [RoleModuleValidation(ModuleConstant.DetailQuiz, ModuleConstant.EditQuiz)]
        [HttpGet("{id}/with-questions")]
        public async Task<BaseResponse> GetQuizWithQuestionsByIdAsync([FromRoute] string id)
        {
            try
            {
                if (id is null)
                {
                    throw new KnownException(ErrorMessageConstant.MethodParameterNull);
                }

                var category = await quizRepository.GetQuizWithQuestionsByIdAsync(id);

                return new BaseResponse(true, "", category);
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

        [RoleModuleValidation(ModuleConstant.EditQuiz)]
        [HttpPut]
        [Route("{id}")]
        public async Task<BaseResponse> UpdateQuizByIdAsync([FromRoute] string? id, [FromBody] QuizDto? quizDto)
        {
            try
            {
                if (quizDto == null || id == null)
                {
                    throw new KnownException(ErrorMessageConstant.MethodParameterNull);
                }

                var validator = new QuizEditValidator();
                var results = validator.Validate(quizDto);
                if (!results.IsValid)
                {
                    var messages = results.Errors.Select(x => x.ErrorMessage).ToList();
                    return new BaseResponse(false, messages);
                }

                var result = await quizRepository.UpdateDataByIdAsync(id, quizDto);

                return new BaseResponse(true, "Kuis berhasil diupdate", result);
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

        [RoleModuleValidation(ModuleConstant.DeleteQuiz)]
        [HttpDelete]
        [Route("{id}")]
        public async Task<BaseResponse> DeleteCategoryByIdAsync([FromRoute] string id)
        {
            try
            {
                await quizRepository.DeleteDataAsync(id);

                return new BaseResponse(true, "Kuis berhasil dihapus", null);
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
        
        [RoleModuleValidation(ModuleConstant.DetailQuiz)]
        [HttpGet("{id}/take-quiz")]
        public async Task<BaseResponse> TakeQuizByIdAsync([FromRoute] string id)
        {
            try
            {
                if (id is null)
                {
                    throw new KnownException(ErrorMessageConstant.MethodParameterNull);
                }

                var category = await quizRepository.TakeQuizByIdAsync(id);

                return new BaseResponse(true, "", category);
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

        [RoleModuleValidation(ModuleConstant.DetailQuiz)]
        [HttpPost]
        [Route("{id}/check-quiz")]
        public async Task<BaseResponse> TakeQuizAsync([FromBody] CheckQuizDto checkQuizDto, [FromRoute] string id)
        {
            try
            {
                var result = await quizRepository.CheckQuizAsync(checkQuizDto, id);

                return new BaseResponse(true, "Hasil kuis berhasil disimpan", result);
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

        [RoleModuleValidation(ModuleConstant.DetailQuiz)]
        [HttpGet]
        [Route("{id}/history")]
        public async Task<BaseResponse> GetHistoriesByQuizIdAsync([FromQuery] SearchRequestDto searchRequest, [FromRoute] string id)
        {
            try
            {
                var result = await quizRepository.GetHistoriesByQuizIdAsync(searchRequest, id);

                return new BaseResponse(true, "", result);
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