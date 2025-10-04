namespace QuizApi.Exceptions
{
    public class KnownException : Exception
    {
        public bool IsSaveToLog { get; set; }

        public KnownException(string? message, bool isSaveToLog = true) : base(message)
        {
            IsSaveToLog = isSaveToLog;
        }

        public KnownException(string? message, Exception innerException, bool isSaveToLog = true) : base(message, innerException)
        {
            IsSaveToLog = isSaveToLog;
        }
    }
}