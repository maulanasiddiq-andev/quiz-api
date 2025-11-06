using System.Net.Http.Headers;
using System.Text.Json;
using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.Options;
using QuizApi.Settings;

namespace QuizApi.Services
{
    public class PushNotificationService
    {
        private readonly PushNotificationSetting pushNotificationSetting;
        public PushNotificationService(IOptions<PushNotificationSetting> options)
        {
            pushNotificationSetting = options.Value;
        }

        public async Task SendNotificationAsync(
            string fcmToken,
            string title,
            string body
        )
        {
            var message = new
            {
                message = new
                {
                    token = fcmToken,
                    notification = new
                    {
                        title,
                        body
                    }
                }
            };

            var json = JsonSerializer.Serialize(message);
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await GetAccessTokenAsync());
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var response = await client.PostAsync(
                $"https://fcm.googleapis.com/v1/projects/{pushNotificationSetting.ProjectId}/messages:send",
                new StringContent(json, System.Text.Encoding.UTF8, "application/json")
            );

            var result = await response.Content.ReadAsStringAsync();
        }

        private async Task<string> GetAccessTokenAsync()
        {
            // redirect the app to firebase service account
            // the file is related to the compiled app
            var path = Path.Combine(AppContext.BaseDirectory, "firebase-service-account.json");

            var credential = GoogleCredential
                .FromFile(path)
                .CreateScoped("https://www.googleapis.com/auth/firebase.messaging");

            var token = await credential.UnderlyingCredential.GetAccessTokenForRequestAsync();
            return token;
        }
    }
}