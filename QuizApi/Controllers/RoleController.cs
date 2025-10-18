using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
    public class RoleController : ControllerBase
    {
        private readonly RoleRepository roleRepository;
        private readonly ActivityLogService activityLogService;
        public RoleController(RoleRepository roleRepository, ActivityLogService activityLogService)
        {
            this.roleRepository = roleRepository;
            this.activityLogService = activityLogService;
        }

        [HttpGet]
        [Route("get-modules")]
        public async Task<BaseResponse> GetAllModules()
        {
            var modules = roleRepository.GetAllModules();

            return new BaseResponse(true, "", modules);
        }

        [HttpGet]
        public async Task<BaseResponse> SearchRolesAsync([FromQuery] SearchRequestDto searchRequest)
        {
            try
            {
                var result = await roleRepository.SearchDatasAsync(searchRequest);

                return new BaseResponse(true, "", result);
            }
            catch (Exception ex)
            {
                activityLogService.SaveErrorLog(ex, this.GetActionName(), this.GetUserId());

                return new BaseResponse(false, ErrorMessageConstant.ServerError, null);
            }
        }

        [HttpPost]
        public async Task<BaseResponse> CreateRoleAsync([FromBody] RoleDto roleDto)
        {
            try
            {
                if (roleDto == null)
                {
                    throw new KnownException(ErrorMessageConstant.MethodParameterNull);
                }

                var validator = new RoleValidator();
                var results = validator.Validate(roleDto);
                if (!results.IsValid)
                {
                    var messages = results.Errors.Select(x => x.ErrorMessage).ToList();
                    return new BaseResponse(false, messages);
                }

                await roleRepository.CreateDataAsync(roleDto);

                return new BaseResponse(true, "Role berhasil ditambahkan", null);
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
        [Route("{id}")]
        public async Task<BaseResponse> GetRoleByIdAsync([FromRoute] string id)
        {
            try
            {
                var result = await roleRepository.GetDataByIdAsync(id);

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

        [HttpGet]
        [Route("{id}/with-modules")]
        public async Task<BaseResponse> GetRoleByIdWithModulesAsync([FromRoute] string id)
        {
            try
            {
                var result = await roleRepository.GetRoleByIdWithModulesAsync(id);

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
        
        [HttpPut]
        [Route("{id}/update-modules")]
        public async Task<BaseResponse> UpdateRoleModulesByIdAsync([FromRoute] string id, [FromBody] RoleWithModuleDto roleWithModuleDto)
        {
            try
            {
                await roleRepository.UpdateRoleModulesByIdAsync(id, roleWithModuleDto);

                return new BaseResponse(true, "Role berhasil diupdate", null);
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