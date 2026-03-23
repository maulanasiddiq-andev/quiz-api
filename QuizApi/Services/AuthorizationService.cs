using Google.Cloud.Firestore;
using QuizApi.Constants;
using QuizApi.DTOs.Auth;
using QuizApi.Models.Auth;

namespace QuizApi.Services
{
    public class AuthorizationService
    {
        private readonly FirestoreDb _firestoreDb;

        public AuthorizationService(FirestoreDb firestoreDb)
        {
            _firestoreDb = firestoreDb;
        }

        public async Task<UserTokenDto?> ValidateTokenAsync(string userId, string? token)
        {
            try
            {
                if (string.IsNullOrEmpty(token)) return null;

                // Query Firestore for the active token matching this user
                Query query = _firestoreDb.Collection("usertoken")
                    .WhereEqualTo("UserId", userId)
                    .WhereEqualTo("Token", token)
                    .WhereEqualTo("RecordStatus", RecordStatusConstant.Active)
                    .Limit(1);

                QuerySnapshot snapshot = await query.GetSnapshotAsync();
                DocumentSnapshot? doc = snapshot.Documents.FirstOrDefault();

                if (doc == null || !doc.Exists) return null;

                var userToken = doc.ConvertTo<UserTokenModel>();
                
                // Firestore returns dates as Timestamps; ensure Model handles conversion
                // Checking if token is expired
                bool isExpired = userToken.ExpiredTime < DateTime.UtcNow;
                bool allowed = userToken.IsAccessAllowed && !isExpired;

                return new UserTokenDto
                {
                    UserTokenId = userToken.UserTokenId,
                    UserId = userToken.UserId,
                    IsAccessAllowed = allowed,
                    ExpiredTime = userToken.ExpiredTime
                };
            }
            catch (Exception)
            {
                // Log exception if you have a logger
                return null;
            }
        }
    }
}