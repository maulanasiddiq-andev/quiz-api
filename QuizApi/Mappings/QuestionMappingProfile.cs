using AutoMapper;
using QuizApi.DTOs.Quiz;
using QuizApi.Models.Quiz;

namespace QuizApi.Mappings
{
    public class QuestionMappingProfile : Profile
    {
        public QuestionMappingProfile()
        {
            CreateMap<QuestionModel, QuestionDto>();
            CreateMap<QuestionDto, QuestionModel>();
        }
    }
}