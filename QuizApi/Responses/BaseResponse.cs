namespace QuizApi.Responses
{
    public class BaseResponse
    {
        public BaseResponse(bool succeed, string? message, object? data)
        {
            Succeed = succeed;
            Messages = new List<string>();
            if (message != null) Messages.Add(message);
            Data = data;
        }

        public BaseResponse(bool succeed, List<string> messages)
        {
            Succeed = succeed;
            Messages = messages;
        }

        public bool Succeed { get; set; }
        public List<string> Messages { get; set; }
        public object? Data { get; set; }
    }
}