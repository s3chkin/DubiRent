using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using Microsoft.Extensions.Configuration;

namespace DubiRent.Services
{
    public interface IEmailService
    {
        Task SendViewingRequestApprovedEmailAsync(string toEmail, string fullName, string propertyTitle, DateTime preferredDate, TimeSpan preferredTime);
    }

    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task SendViewingRequestApprovedEmailAsync(string toEmail, string fullName, string propertyTitle, DateTime preferredDate, TimeSpan preferredTime)
        {
            try
            {
                var smtpHost = _configuration["EmailSettings:SmtpHost"] ?? "smtp.gmail.com";
                var smtpPort = int.Parse(_configuration["EmailSettings:SmtpPort"] ?? "587");
                var smtpUsername = _configuration["EmailSettings:SmtpUsername"];
                var smtpPassword = _configuration["EmailSettings:SmtpPassword"];
                var fromEmail = _configuration["EmailSettings:FromEmail"] ?? smtpUsername;
                var fromName = _configuration["EmailSettings:FromName"] ?? "DubiRent";

                if (string.IsNullOrEmpty(smtpUsername) || string.IsNullOrEmpty(smtpPassword))
                {
                    _logger.LogWarning("Email settings not configured. Email not sent.");
                    return;
                }

                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(fromName, fromEmail));
                message.To.Add(new MailboxAddress(fullName, toEmail));
                message.Subject = "Your Viewing Request Has Been Approved - DubiRent";

                var bodyBuilder = new BodyBuilder
                {
                    HtmlBody = GetApprovalEmailHtml(fullName, propertyTitle, preferredDate, preferredTime)
                };

                message.Body = bodyBuilder.ToMessageBody();

                using (var client = new SmtpClient())
                {
                    await client.ConnectAsync(smtpHost, smtpPort, SecureSocketOptions.StartTls);
                    await client.AuthenticateAsync(smtpUsername, smtpPassword);
                    await client.SendAsync(message);
                    await client.DisconnectAsync(true);
                }

                _logger.LogInformation($"Approval email sent successfully to {toEmail}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send approval email to {toEmail}");
                // Don't throw - we don't want email failures to break the request approval
            }
        }

        private string GetApprovalEmailHtml(string fullName, string propertyTitle, DateTime preferredDate, TimeSpan preferredTime)
        {
            var dateFormatted = preferredDate.ToString("dddd, MMMM dd, yyyy");
            var timeFormatted = preferredTime.ToString(@"hh\:mm");

            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <style>
        body {{
            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
            line-height: 1.6;
            color: #333;
            max-width: 600px;
            margin: 0 auto;
            padding: 20px;
            background-color: #f4f4f4;
        }}
        .container {{
            background-color: white;
            border-radius: 10px;
            padding: 30px;
            box-shadow: 0 2px 10px rgba(0,0,0,0.1);
        }}
        .header {{
            background: linear-gradient(135deg, #fe5658 0%, #ff6b6d 100%);
            color: white;
            padding: 20px;
            border-radius: 10px 10px 0 0;
            margin: -30px -30px 30px -30px;
            text-align: center;
        }}
        .header h1 {{
            margin: 0;
            font-size: 24px;
        }}
        .content {{
            margin: 20px 0;
        }}
        .property-info {{
            background-color: #f9fafb;
            padding: 15px;
            border-radius: 8px;
            border-left: 4px solid #fe5658;
            margin: 20px 0;
        }}
        .property-title {{
            font-size: 18px;
            font-weight: bold;
            color: #1f2937;
            margin-bottom: 10px;
        }}
        .details {{
            margin: 15px 0;
        }}
        .detail-row {{
            margin: 10px 0;
            padding: 8px 0;
            border-bottom: 1px solid #e5e7eb;
        }}
        .detail-label {{
            font-weight: 600;
            color: #6b7280;
            display: inline-block;
            width: 120px;
        }}
        .detail-value {{
            color: #1f2937;
        }}
        .footer {{
            margin-top: 30px;
            padding-top: 20px;
            border-top: 1px solid #e5e7eb;
            text-align: center;
            color: #6b7280;
            font-size: 14px;
        }}
        .btn {{
            display: inline-block;
            padding: 12px 24px;
            background: linear-gradient(135deg, #fe5658 0%, #ff6b6d 100%);
            color: white;
            text-decoration: none;
            border-radius: 8px;
            margin: 20px 0;
            font-weight: 600;
        }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>✓ Viewing Request Approved</h1>
        </div>
        <div class='content'>
            <p>Dear {fullName},</p>
            <p>Great news! Your viewing request has been <strong>approved</strong>. We're excited to show you the property.</p>
            
            <div class='property-info'>
                <div class='property-title'>{propertyTitle}</div>
            </div>
            
            <div class='details'>
                <div class='detail-row'>
                    <span class='detail-label'>Date:</span>
                    <span class='detail-value'>{dateFormatted}</span>
                </div>
                <div class='detail-row'>
                    <span class='detail-label'>Time:</span>
                    <span class='detail-value'>{timeFormatted}</span>
                </div>
            </div>
            
            <p>Please arrive on time for your viewing appointment. If you need to reschedule or have any questions, please contact us as soon as possible.</p>
            
            <p style='margin-top: 30px;'>We look forward to meeting you!</p>
            <p>Best regards,<br><strong>The DubiRent Team</strong></p>
        </div>
        <div class='footer'>
            <p>© {DateTime.Now.Year} DubiRent. All rights reserved.</p>
            <p>This is an automated message, please do not reply to this email.</p>
        </div>
    </div>
</body>
</html>";
        }
    }
}

