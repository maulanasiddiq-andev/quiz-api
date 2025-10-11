using AutoMapper;
using QuizApi.DTOs.CheckQuiz;
using QuizApi.DTOs.QuizHistory;
using QuizApi.Models.Quiz;
using QuizApi.Models.QuizHistory;

namespace QuizApi.Mappings
{
    public class QuizHistoryMappingProfile : Profile
    {
        public QuizHistoryMappingProfile()
        {
            CreateMap<QuizHistoryModel, QuizHistoryDto>();
            CreateMap<QuestionHistoryModel, QuestionHistoryDto>();
            CreateMap<AnswerHistoryModel, AnswerHistoryDto>();

            CreateMap<QuizModel, QuizHistoryModel>();
            CreateMap<QuestionModel, QuestionHistoryModel>();
            CreateMap<AnswerModel, AnswerHistoryModel>();
        }
    }
}