using AutoMapper;
using QuizApi.DTOs.Auth;
using QuizApi.DTOs.Identity;
using QuizApi.Models.Identity;

namespace QuizApi.Mappings
{
    public class UserMappingProfile : Profile
    {
        public UserMappingProfile()
        {
            CreateMap<RegisterDto, UserModel>();
            CreateMap<UserModel, UserDto>();
            CreateMap<UserModel, SimpleUserDto>();
        }
    }
}