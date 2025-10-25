using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuizApi.Attributes;
using QuizApi.Constants;
using QuizApi.DTOs.Quiz;
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
    public class CategoryController : ControllerBase
    {
        private readonly CategoryRepository categoryRepository;
        private readonly ActivityLogService activityLogService;
        public CategoryController(
            CategoryRepository categoryRepository,
            ActivityLogService activityLogService
        )
        {
            this.categoryRepository = categoryRepository;
            this.activityLogService = activityLogService;
        }

        [HttpPost]
        public async Task<BaseResponse> CreateCategoryAsync([FromBody] CategoryDto category)
        {
            try
            {
                var validator = new CategoryValidator();
                var results = validator.Validate(category);
                if (!results.IsValid)
                {
                    var messages = results.Errors.Select(x => x.ErrorMessage).ToList();
                    return new BaseResponse(false, messages);
                }

                await categoryRepository.CreateDataAsync(category);

                return new BaseResponse(true, "Kategori berhasil ditambahkan", null);
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

        [HttpGet]
        public async Task<BaseResponse> SearchCategoriesAsync([FromQuery] SearchRequestDto searchRequest)
        {
            try
            {
                var result = await categoryRepository.SearchDatasAsync(searchRequest);

                return new BaseResponse(true, "", result);
            }
            catch (Exception ex)
            {
                activityLogService.SaveErrorLog(ex, this.GetActionName(), this.GetUserId());

                return new BaseResponse(false, ErrorMessageConstant.ServerError, null);
            }
        }

        [HttpGet("{id}")]
        public async Task<BaseResponse> GetCategoryByIdAsync([FromRoute] string id)
        {
            try
            {
                if (id is null)
                {
                    throw new KnownException(ErrorMessageConstant.MethodParameterNull);
                }

                var category = await categoryRepository.GetDataByIdAsync(id);

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

        [HttpPut]
        [Route("{id}")]
        public async Task<BaseResponse> UpdateCategoryByIdAsync([FromRoute] string id, [FromBody] CategoryDto category)
        {
            try
            {
                if (category is null || id is null)
                {
                    throw new KnownException(ErrorMessageConstant.MethodParameterNull);
                }

                var validator = new CategoryUpdateValidator();
                var results = validator.Validate(category);
                if (!results.IsValid)
                {
                    var messages = results.Errors.Select(x => x.ErrorMessage).ToList();
                    return new BaseResponse(false, messages);
                }

                var result = await categoryRepository.UpdateDataAsync(id, category);

                return new BaseResponse(true, "category berhasil diupdate", result);
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

        [HttpDelete]
        [Route("{id}")]
        public async Task<BaseResponse> DeleteCategoryByIdAsync([FromRoute] string id)
        {
            try
            {
                await categoryRepository.DeleteDataAsync(id);

                return new BaseResponse(true, "Kategori berhasil dihapus", null);
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