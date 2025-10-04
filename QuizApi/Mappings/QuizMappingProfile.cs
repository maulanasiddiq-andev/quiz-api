using AutoMapper;
using QuizApi.DTOs.Quiz;
using QuizApi.Models.Quiz;

namespace QuizApi.Mappings
{
    public class QuizMappingProfile : Profile
    {
        public QuizMappingProfile()
        {
            CreateMap<QuizModel, QuizDto>();
            CreateMap<QuizDto, QuizModel>();
        }
    }
}