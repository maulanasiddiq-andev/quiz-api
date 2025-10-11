using AutoMapper;
using QuizApi.DTOs.CheckQuiz;
using QuizApi.DTOs.QuizHistory;
using QuizApi.Models.QuizHistory;

namespace QuizApi.Mappings
{
    public class QuizHistoryMappingProfile : Profile
    {
        public QuizHistoryMappingProfile()
        {
            CreateMap<CheckQuizDto, QuizHistoryModel>();
            CreateMap<CheckQuestionDto, QuestionHistoryModel>();
            CreateMap<CheckAnswerDto, AnswerHistoryModel>();

            CreateMap<QuizHistoryModel, QuizHistoryDto>();
            CreateMap<QuestionHistoryModel, QuestionHistoryDto>();
            CreateMap<AnswerHistoryModel, AnswerHistoryDto>();
        }
    }
}