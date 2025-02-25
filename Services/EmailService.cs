using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using System;

namespace LeadManagement.Services {
    public class EmailService {
        private readonly string _smtpHost;
        private readonly int _smtpPort;
        private readonly string _username;
        private readonly string _password;
        private readonly string _fromEmail;

        public EmailService(IConfiguration configuration) {
            _smtpHost = configuration["SmtpSettings:Host"];
            _smtpPort = int.Parse(configuration["SmtpSettings:Port"]);
            _username = configuration["SmtpSettings:Username"];
            _password = configuration["SmtpSettings:Password"];
            _fromEmail = configuration["SmtpSettings:FromEmail"];
        }

        public void SendEmail(string toEmail, string subject, string body) {
            try {
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress("Lead Management", _fromEmail));
                message.To.Add(new MailboxAddress("", toEmail));
                message.Subject = subject;
                message.Body = new TextPart("html") { Text = body };

                using (var client = new SmtpClient()) {
                    client.Connect(_smtpHost, _smtpPort, SecureSocketOptions.StartTls);
                    client.Authenticate(_username, _password); 
                    client.Send(message);
                    client.Disconnect(true);
                }

                Console.WriteLine($"Email sent successfully to {toEmail}");
            } catch (Exception ex) {
                Console.WriteLine($"Email failed: {ex.Message}");
            }
        }
    }
}
