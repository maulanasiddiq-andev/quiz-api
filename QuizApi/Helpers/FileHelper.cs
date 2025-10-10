using Google.Cloud.Storage.V1;

namespace QuizApi.Helpers
{
    public class FileHelper
    {
        private readonly StorageClient _storageClient;
        private readonly string bucketName = "maulana-developer-project-quiz-bucket";
        public FileHelper()
        {
            _storageClient = StorageClient.Create();
        }

        public async Task<string> SaveFile(IFormFile file, string directory)
        {
            var fileName = Guid.NewGuid() + Path.GetExtension(file.FileName);
            var objectName = $"{directory}/{fileName}";
            using (var memoryStream = new MemoryStream())
            {
                await file.CopyToAsync(memoryStream);
                memoryStream.Position = 0;

                var obj = await _storageClient.UploadObjectAsync(
                    bucketName,
                    objectName,
                    file.ContentType,
                    memoryStream
                );

                return $"https://storage.googleapis.com/{bucketName}/{objectName}";
            }
        }

        public async Task RemoveFile(string filePath)
        {
            string[] splittedTexts = filePath.Split("/");
            int length = splittedTexts.Count();
            if (length > 0)
            {
                string[] splittedObjectName = { splittedTexts[length - 2], splittedTexts[length - 1] };
                string objectName = string.Join("/", splittedObjectName);

                await _storageClient.DeleteObjectAsync(bucketName, objectName);
            }
        }
    }
}