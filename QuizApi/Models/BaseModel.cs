using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuizApi.Models
{
    public class BaseModel
    {
        [Timestamp]
        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        [Column("xmin", TypeName = "xid")]
        public uint Version { get; set; }
        public string Description { get; set; } = string.Empty;
        public string RecordStatus { get; set; } = string.Empty;
        public DateTime? CreatedTime { get; set; } = null;        
        public DateTime? ModifiedTime { get; set; } = null;
        public DateTime? DeletedTime { get; set; } = null;
        public string CreatedBy { get; set; } = string.Empty;        
        public string ModifiedBy { get; set; } = string.Empty;        
        public string? DeletedBy { get; set; }        
    }
}