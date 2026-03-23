using MailKit.Net.Smtp;
using Microsoft.Extensions.Options;
using MimeKit;
using QuizApi.Settings;

namespace QuizApi.Services
{
    public class EmailService
    {
        private readonly EmailSetting emailSetting;
        public EmailService(IOptions<EmailSetting> emailOptions)
        {
            emailSetting = emailOptions.Value;
        }

        public async Task SendEmailAsync(string name, string email, string text)
        {
            // plug your SMTP provider here
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("Everyday Quiz", "maulanasiddiqdeveloper@gmail.com"));
            message.To.Add(new MailboxAddress(name, email));
            message.Subject = "Kode OTP";

            message.Body = new TextPart("html")
            {
                Text = $@"
                <!DOCTYPE html>
                <html>
                    <body style='font-family:Arial, sans-serif; background:#f4f4f4; padding:20px;'>

                    <div style='max-width:600px; margin:0 auto; background:#ffffff; padding:20px; border-radius:10px;'>
                        <h2 style='color:#333;'>Hai {name},</h2>

                        <p style='color:#555; line-height:1.6;'>
                            {text}
                        </p>

                        <p style='font-size:12px; color:#888; margin-top:25px;'>
                            — Everyday Quiz
                        </p>
                    </div>

                    </body>
                </html>"
            };

            using var client = new SmtpClient
            {
                Timeout = 20_000
            };

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

            await client.ConnectAsync(
                emailSetting.SmtpServer, 
                emailSetting.Port, 
                MailKit.Security.SecureSocketOptions.StartTls,
                cts.Token    
            );

            await client.AuthenticateAsync(
                emailSetting.Username, 
                emailSetting.Password,
                cts.Token
            );

            await client.SendAsync(message, cts.Token);

            await client.DisconnectAsync(true, cts.Token);
        }
    }
}