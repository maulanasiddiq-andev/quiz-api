using AutoMapper;
using QuizApi.DTOs.Quiz;
using QuizApi.Models.Quiz;

namespace QuizApi.Mappings
{
    public class CategoryMappingProfile : Profile
    {
        public CategoryMappingProfile()
        {
            CreateMap<CategoryDto, CategoryModel>();
            CreateMap<CategoryModel, CategoryDto>();
        }
    }
}