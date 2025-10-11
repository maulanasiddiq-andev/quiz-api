using AutoMapper;
using QuizApi.DTOs.Quiz;
using QuizApi.DTOs.TakeQuiz;
using QuizApi.Models.Quiz;

namespace QuizApi.Mappings
{
    public class QuizMappingProfile : Profile
    {
        public QuizMappingProfile()
        {
            CreateMap<QuizModel, QuizDto>();
            CreateMap<QuizDto, QuizModel>();
            CreateMap<QuizModel, TakeQuizDto>();

            CreateMap<QuestionModel, QuestionDto>();
            CreateMap<QuestionDto, QuestionModel>();
            CreateMap<QuestionModel, TakeQuestionDto>();

            CreateMap<AnswerModel, AnswerDto>();
            CreateMap<AnswerDto, AnswerModel>();
            CreateMap<AnswerModel, TakeAnswerDto>();
        }
    }
}