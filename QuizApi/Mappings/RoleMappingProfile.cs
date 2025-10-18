using AutoMapper;
using QuizApi.DTOs.Identity;
using QuizApi.Models.Identity;

namespace QuizApi.Mappings
{
    public class RoleMappingProfile : Profile
    {
        public RoleMappingProfile()
        {
            CreateMap<RoleDto, RoleModel>();
            CreateMap<RoleModel, RoleDto>();
            CreateMap<RoleModel, RoleWithModuleDto>();
        }
    }
}