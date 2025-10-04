namespace QuizApi.DTOs
{
    public class BaseDto
    {
        public uint? Version { get; set; }
        public string Description { get; set; } = string.Empty;
        public string? RecordStatus { get; set; }
        public DateTime? CreatedTime { get; set; }
        public DateTime? ModifiedTime { get; set; }
        public DateTime? DeletedTime { get; set; }
        public string? CreatedBy { get; set; }
        public string? ModifiedBy { get; set; }
        public string? DeletedBy { get; set; }
    }
}