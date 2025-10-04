using AutoMapper;
using QuizApi.DTOs.Quiz;
using QuizApi.Models.Quiz;

namespace QuizApi.Mappings
{
    public class AnswerMappingProfile : Profile
    {
        public AnswerMappingProfile()
        {
            CreateMap<AnswerModel, AnswerDto>();
            CreateMap<AnswerDto, AnswerModel>();
        }
    }
}