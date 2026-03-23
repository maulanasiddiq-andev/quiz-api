using Google.Cloud.Firestore;

namespace QuizApi.Models
{
    [FirestoreData]
    public class BaseModel
    {
        // Replace PostgreSQL 'xmin' versioning. 
        // Firestore tracks 'UpdateTime' automatically on every write.
        [FirestoreDocumentUpdateTimestamp] 
        public Timestamp? Version { get; set; }

        [FirestoreProperty]
        public string Description { get; set; } = string.Empty;

        [FirestoreProperty]
        public string RecordStatus { get; set; } = string.Empty;

        [FirestoreProperty]
        public DateTime? CreatedTime { get; set; }

        [FirestoreProperty]
        public DateTime? ModifiedTime { get; set; }

        [FirestoreProperty]
        public DateTime? DeletedTime { get; set; }

        [FirestoreProperty]
        public string CreatedBy { get; set; } = string.Empty;

        [FirestoreProperty]
        public string ModifiedBy { get; set; } = string.Empty;

        [FirestoreProperty]
        public string? DeletedBy { get; set; }
    }
}