using System.Net.Mail;

namespace leo.Services
{
    public interface IEmailService
    {
        Task<bool> SendEmailAsync(string toEmail, string subject, string message);
    }

    public class EmailService : IEmailService
    {
        private readonly SmtpClient _smtpClient;

        public EmailService(SmtpClient smtpClient)
        {
            _smtpClient = smtpClient;
        }

        public async Task<bool> SendEmailAsync(string toEmail, string subject, string message)
        {
            try
            {
                var mailMessage = new MailMessage("your-email@example.com", toEmail, subject, message);
                await _smtpClient.SendMailAsync(mailMessage);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
