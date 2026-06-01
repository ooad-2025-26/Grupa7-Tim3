using Microsoft.AspNetCore.Identity.UI.Services;
using System.Net;
using System.Net.Mail;

namespace ParkirajBa.Services
{
    public class EmailSender : IEmailSender
    {
        private readonly IConfiguration _config;
        private readonly ILogger<EmailSender> _logger;

        public EmailSender(IConfiguration config, ILogger<EmailSender> logger)
        {
            _config = config;
            _logger = logger;
        }

        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            try
            {
                var fromEmail = _config["Email:Address"] ?? "parkirajba.service@gmail.com";
                var password = _config["Email:Password"] ?? "iplx fham rnwz oajz";

                var smtpClient = new SmtpClient("smtp.gmail.com")
                {
                    Port = 587,
                    Credentials = new NetworkCredential(fromEmail, password),
                    EnableSsl = true,
                };

                var mail = new MailMessage
                {
                    From = new MailAddress(fromEmail, "ParkirajBa"),
                    Subject = subject,
                    Body = htmlMessage,
                    IsBodyHtml = true,
                };

                mail.To.Add(email);
                await smtpClient.SendMailAsync(mail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Greška pri slanju emaila na {Email}", email);
            }
        }
    }
}
