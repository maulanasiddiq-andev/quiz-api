using MailKit.Net.Smtp;
using Microsoft.Extensions.Options;
using MimeKit;
using QuizApi.Settings;

namespace QuizApi.Services
{
    public class EmailService
    {
        private readonly EmailSetting emailSetting;
        public EmailService(
            IOptions<EmailSetting> emailOptions
        )
        {
            emailSetting = emailOptions.Value;
        }

        public async Task SendEmailAsync(
            string name,
            string email,
            string text
        )
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("MS Developer Video App", "maulanasiddiqdeveloper@gmail.com"));
            message.To.Add(new MailboxAddress(name, email));
            message.Subject = "Kode OTP";

            message.Body = new TextPart("plain")
            {
                Text = text
            };

            using (var client = new SmtpClient())
            {
                client.Connect(emailSetting.SmtpServer, emailSetting.Port, MailKit.Security.SecureSocketOptions.StartTls);
                client.Authenticate(emailSetting.Username, emailSetting.Password);

                client.Send(message);
                client.Disconnect(true);
            }
        }
    }
}