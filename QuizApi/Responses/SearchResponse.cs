namespace QuizApi.Responses
{
    public class SearchResponse
    {
        public object? Items { get; set; }
        public int TotalItems { get; set; }
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
        public int TotalPages
        {
            get
            {
                return (int)Math.Ceiling(TotalItems / (double)PageSize);
            }
        }
        public bool HasPreviousPage => CurrentPage > 1;
        public bool HasNextPage => (TotalPages - 1) > CurrentPage;    
    }
}