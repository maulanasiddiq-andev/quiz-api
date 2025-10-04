using AutoMapper;
using QuizApi.DTOs.QuizExam;
using QuizApi.DTOs.QuizHistory;
using QuizApi.Models.QuizHistory;

namespace QuizApi.Mappings
{
    public class QuizHistoryMappingProfile : Profile
    {
        public QuizHistoryMappingProfile()
        {
            CreateMap<QuizExamDto, QuizHistoryModel>();
            CreateMap<QuestionExamDto, QuestionHistoryModel>();
            CreateMap<AnswerExamDto, AnswerHistoryModel>();

            CreateMap<QuizHistoryModel, QuizHistoryDto>();
            CreateMap<QuestionHistoryModel, QuestionHistoryDto>();
            CreateMap<AnswerHistoryModel, AnswerHistoryDto>();
        }
    }
}