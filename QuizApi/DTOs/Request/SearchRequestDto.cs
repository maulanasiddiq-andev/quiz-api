namespace QuizApi.DTOs.Request
{
    public class SearchRequestDto
    {
        public int PageSize { get; set; } = 10;
        public int CurrentPage { get; set; } = 0;
        public string OrderBy { get; set; } = "createdTime";
        public string OrderDir { get; set; } = "desc";
    }
}