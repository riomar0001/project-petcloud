using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace PurrVet.Services {
    public class EmailService {
        private readonly GmailSettings _settings;

        public EmailService(IOptions<GmailSettings> settings) {
            _settings = settings.Value;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string plainTextBody, string? htmlBody = null) {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_settings.DisplayName ?? _settings.Email, _settings.Email));
            message.To.Add(MailboxAddress.Parse(toEmail));
            message.Subject = subject;

            var builder = new BodyBuilder {
                TextBody = plainTextBody,
                HtmlBody = htmlBody
            };
            message.Body = builder.ToMessageBody();

            using var smtp = new SmtpClient();

            smtp.ServerCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => {
                if (certificate is X509Certificate2 cert && cert.Subject.Contains("smtp.gmail.com"))
                    return true;

                return sslPolicyErrors == SslPolicyErrors.None;
            };

            try {
                if (_settings.UseStartTls) {
                    await smtp.ConnectAsync(_settings.SmtpHost, _settings.SmtpPort, SecureSocketOptions.StartTls);
                } else {
                    await smtp.ConnectAsync(_settings.SmtpHost, _settings.SmtpPort, SecureSocketOptions.SslOnConnect);
                }

                await smtp.AuthenticateAsync(_settings.Email, _settings.AppPassword);
                await smtp.SendAsync(message);
            } finally {
                if (smtp.IsConnected)
                    await smtp.DisconnectAsync(true);
            }
        }
    }
}
