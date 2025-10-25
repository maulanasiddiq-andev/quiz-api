using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuizApi.Attributes;
using QuizApi.Constants;
using QuizApi.DTOs.Request;
using QuizApi.Exceptions;
using QuizApi.Extensions;
using QuizApi.Helpers;
using QuizApi.Responses;
using QuizApi.Services;

namespace QuizApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    [TokenValidation]
    public class FileController : ControllerBase
    {
        private readonly FileHelper fileHelper;
        private readonly ActivityLogService activityLogService;
        public FileController(ActivityLogService activityLogService)
        {
            this.activityLogService = activityLogService;
            fileHelper = new FileHelper();
        }

        [HttpPost]
        [Route("upload-image")]
        public async Task<BaseResponse> UploadQuizImageAsync([FromForm] UploadImageDto uploadQuizImageDto)
        {
            try
            {
                if (uploadQuizImageDto == null)
                {
                    throw new KnownException(ErrorMessageConstant.MethodParameterNull);
                }

                var validator = new UploadImageValidator();
                var results = validator.Validate(uploadQuizImageDto);
                if (!results.IsValid)
                {
                    var messages = results.Errors.Select(x => x.ErrorMessage).ToList();
                    return new BaseResponse(false, messages);
                }

                var imageUrl = await fileHelper.SaveFile(uploadQuizImageDto.Image!, uploadQuizImageDto.Directory);
                return new BaseResponse(true, "", imageUrl);
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