using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace DubiRent.Services
{
    public interface IEmailService
    {
        Task SendViewingRequestApprovedEmailAsync(string toEmail, string fullName, string propertyTitle, DateTime preferredDate, TimeSpan preferredTime);
        Task SendViewingRequestStatusUpdateEmailAsync(string toEmail, string fullName, string propertyTitle, DateTime preferredDate, TimeSpan preferredTime, string status);
        Task SendPasswordResetEmailAsync(string toEmail, string userName, string resetLink);
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
                    // Handle SSL certificate validation issues
                    // Use Auto to allow MailKit to choose the best secure option
                    client.ServerCertificateValidationCallback = (s, c, h, e) => true;
                    
                    // Try StartTls first, fallback to SSL if needed
                    SecureSocketOptions socketOptions = smtpPort == 465 
                        ? SecureSocketOptions.SslOnConnect 
                        : SecureSocketOptions.StartTls;
                    
                    await client.ConnectAsync(smtpHost, smtpPort, socketOptions);
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

        public async Task SendViewingRequestStatusUpdateEmailAsync(string toEmail, string fullName, string propertyTitle, DateTime preferredDate, TimeSpan preferredTime, string status)
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
                
                // Set subject and body based on status
                string subject = "";
                string htmlBody = "";
                
                switch (status.ToLower())
                {
                    case "approved":
                        subject = "Your Viewing Request Has Been Approved - DubiRent";
                        htmlBody = GetApprovalEmailHtml(fullName, propertyTitle, preferredDate, preferredTime);
                        break;
                    case "completed":
                        subject = "Viewing Request Completed - DubiRent";
                        htmlBody = GetCompletedEmailHtml(fullName, propertyTitle, preferredDate, preferredTime);
                        break;
                    case "cancelled":
                        subject = "Viewing Request Cancelled - DubiRent";
                        htmlBody = GetCancelledEmailHtml(fullName, propertyTitle, preferredDate, preferredTime);
                        break;
                    case "pending":
                    default:
                        subject = "Viewing Request Status Update - DubiRent";
                        htmlBody = GetStatusUpdateEmailHtml(fullName, propertyTitle, preferredDate, preferredTime, status);
                        break;
                }

                message.Subject = subject;

                var bodyBuilder = new BodyBuilder
                {
                    HtmlBody = htmlBody
                };

                message.Body = bodyBuilder.ToMessageBody();

                using (var client = new SmtpClient())
                {
                    // Handle SSL certificate validation issues
                    client.ServerCertificateValidationCallback = (s, c, h, e) => true;
                    
                    // Try StartTls first, fallback to SSL if needed
                    SecureSocketOptions socketOptions = smtpPort == 465 
                        ? SecureSocketOptions.SslOnConnect 
                        : SecureSocketOptions.StartTls;
                    
                    await client.ConnectAsync(smtpHost, smtpPort, socketOptions);
                    await client.AuthenticateAsync(smtpUsername, smtpPassword);
                    await client.SendAsync(message);
                    await client.DisconnectAsync(true);
                }

                _logger.LogInformation($"Status update email sent successfully to {toEmail} for status: {status}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send status update email to {toEmail}");
                // Don't throw - we don't want email failures to break the status update
            }
        }

        public async Task SendPasswordResetEmailAsync(string toEmail, string userName, string resetLink)
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
                    _logger.LogError("Email settings not configured. SmtpUsername or SmtpPassword is missing in appsettings.json");
                    throw new InvalidOperationException("Email settings are not configured. Please configure EmailSettings:SmtpUsername and EmailSettings:SmtpPassword in appsettings.json");
                }

                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(fromName, fromEmail));
                message.To.Add(new MailboxAddress(userName, toEmail));
                message.Subject = "Reset Your Password - DubiRent";

                var bodyBuilder = new BodyBuilder
                {
                    HtmlBody = GetPasswordResetEmailHtml(userName, resetLink)
                };

                message.Body = bodyBuilder.ToMessageBody();

                using (var client = new SmtpClient())
                {
                    // Handle SSL certificate validation issues
                    // Use Auto to allow MailKit to choose the best secure option
                    client.ServerCertificateValidationCallback = (s, c, h, e) => true;
                    
                    // Try StartTls first, fallback to SSL if needed
                    SecureSocketOptions socketOptions = smtpPort == 465 
                        ? SecureSocketOptions.SslOnConnect 
                        : SecureSocketOptions.StartTls;
                    
                    await client.ConnectAsync(smtpHost, smtpPort, socketOptions);
                    await client.AuthenticateAsync(smtpUsername, smtpPassword);
                    await client.SendAsync(message);
                    await client.DisconnectAsync(true);
                }

                _logger.LogInformation($"Password reset email sent successfully to {toEmail}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send password reset email to {toEmail}");
                throw; // Re-throw for password reset to handle errors appropriately
            }
        }

        private string GetPasswordResetEmailHtml(string userName, string resetLink)
        {
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
        .btn {{
            display: inline-block;
            padding: 14px 28px;
            background: linear-gradient(135deg, #fe5658 0%, #ff6b6d 100%);
            color: white !important;
            text-decoration: none;
            border-radius: 8px;
            margin: 25px 0;
            font-weight: 600;
            text-align: center;
        }}
        .btn:hover {{
            background: linear-gradient(135deg, #ff6b6d 0%, #ff7b7d 100%);
        }}
        .info-box {{
            background-color: #fff7ed;
            border-left: 4px solid #f59e0b;
            padding: 15px;
            border-radius: 8px;
            margin: 20px 0;
        }}
        .info-text {{
            color: #92400e;
            font-size: 14px;
            margin: 0;
        }}
        .footer {{
            margin-top: 30px;
            padding-top: 20px;
            border-top: 1px solid #e5e7eb;
            text-align: center;
            color: #6b7280;
            font-size: 14px;
        }}
        .link-text {{
            word-break: break-all;
            color: #6b7280;
            font-size: 12px;
            margin-top: 15px;
        }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>🔐 Reset Your Password</h1>
        </div>
        <div class='content'>
            <p>Hello {userName},</p>
            <p>We received a request to reset your password for your DubiRent account.</p>
            <p>Click the button below to reset your password:</p>
            <div style='text-align: center;'>
                <a href='{resetLink}' class='btn'>Reset Password</a>
            </div>
            <p>Or copy and paste this link into your browser:</p>
            <p class='link-text'>{resetLink}</p>
            <div class='info-box'>
                <p class='info-text'>
                    <strong>⚠️ Security Notice:</strong> This link will expire in 24 hours. If you didn't request a password reset, please ignore this email or contact us if you have concerns.
                </p>
            </div>
            <p>If you're having trouble clicking the button, copy and paste the URL above into your web browser.</p>
            <p style='margin-top: 30px;'>Best regards,<br><strong>The DubiRent Team</strong></p>
        </div>
        <div class='footer'>
            <p>© {DateTime.Now.Year} DubiRent. All rights reserved.</p>
            <p>This is an automated message, please do not reply to this email.</p>
        </div>
    </div>
</body>
</html>";
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

        private string GetCompletedEmailHtml(string fullName, string propertyTitle, DateTime preferredDate, TimeSpan preferredTime)
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
            background: linear-gradient(135deg, #10b981 0%, #059669 100%);
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
            border-left: 4px solid #10b981;
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
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>✓ Viewing Request Completed</h1>
        </div>
        <div class='content'>
            <p>Dear {fullName},</p>
            <p>Thank you for viewing the property with us! Your viewing request has been marked as <strong>completed</strong>.</p>
            
            <div class='property-info'>
                <div class='property-title'>{propertyTitle}</div>
            </div>
            
            <div class='details'>
                <div class='detail-row'>
                    <span class='detail-label'>Viewing Date:</span>
                    <span class='detail-value'>{dateFormatted}</span>
                </div>
                <div class='detail-row'>
                    <span class='detail-label'>Time:</span>
                    <span class='detail-value'>{timeFormatted}</span>
                </div>
            </div>
            
            <p>We hope you had a great experience viewing the property. If you have any questions or would like to proceed with renting this property, please don't hesitate to contact us.</p>
            
            <p style='margin-top: 30px;'>Best regards,<br><strong>The DubiRent Team</strong></p>
        </div>
        <div class='footer'>
            <p>© {DateTime.Now.Year} DubiRent. All rights reserved.</p>
            <p>This is an automated message, please do not reply to this email.</p>
        </div>
    </div>
</body>
</html>";
        }

        private string GetCancelledEmailHtml(string fullName, string propertyTitle, DateTime preferredDate, TimeSpan preferredTime)
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
            background: linear-gradient(135deg, #ef4444 0%, #dc2626 100%);
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
            border-left: 4px solid #ef4444;
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
        .info-box {{
            background-color: #fef2f2;
            border-left: 4px solid #ef4444;
            padding: 15px;
            border-radius: 8px;
            margin: 20px 0;
        }}
        .info-text {{
            color: #991b1b;
            font-size: 14px;
            margin: 0;
        }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>✗ Viewing Request Cancelled</h1>
        </div>
        <div class='content'>
            <p>Dear {fullName},</p>
            <p>We regret to inform you that your viewing request has been <strong>cancelled</strong>.</p>
            
            <div class='property-info'>
                <div class='property-title'>{propertyTitle}</div>
            </div>
            
            <div class='details'>
                <div class='detail-row'>
                    <span class='detail-label'>Scheduled Date:</span>
                    <span class='detail-value'>{dateFormatted}</span>
                </div>
                <div class='detail-row'>
                    <span class='detail-label'>Time:</span>
                    <span class='detail-value'>{timeFormatted}</span>
                </div>
            </div>
            
            <div class='info-box'>
                <p class='info-text'>
                    <strong>Note:</strong> If you would like to reschedule your viewing or have any questions, please feel free to contact us. We're here to help you find your perfect property.
                </p>
            </div>
            
            <p>We apologize for any inconvenience this may cause. If you're still interested in viewing properties, please don't hesitate to make a new viewing request.</p>
            
            <p style='margin-top: 30px;'>Best regards,<br><strong>The DubiRent Team</strong></p>
        </div>
        <div class='footer'>
            <p>© {DateTime.Now.Year} DubiRent. All rights reserved.</p>
            <p>This is an automated message, please do not reply to this email.</p>
        </div>
    </div>
</body>
</html>";
        }

        private string GetStatusUpdateEmailHtml(string fullName, string propertyTitle, DateTime preferredDate, TimeSpan preferredTime, string status)
        {
            var dateFormatted = preferredDate.ToString("dddd, MMMM dd, yyyy");
            var timeFormatted = preferredTime.ToString(@"hh\:mm");
            var statusFormatted = char.ToUpper(status[0]) + status.Substring(1).ToLower();

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
        .status-badge {{
            display: inline-block;
            padding: 6px 12px;
            background-color: #fef3c7;
            color: #92400e;
            border-radius: 6px;
            font-weight: 600;
            margin: 10px 0;
        }}
        .footer {{
            margin-top: 30px;
            padding-top: 20px;
            border-top: 1px solid #e5e7eb;
            text-align: center;
            color: #6b7280;
            font-size: 14px;
        }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>📧 Viewing Request Status Update</h1>
        </div>
        <div class='content'>
            <p>Dear {fullName},</p>
            <p>This is to inform you that the status of your viewing request has been updated.</p>
            
            <div class='property-info'>
                <div class='property-title'>{propertyTitle}</div>
            </div>
            
            <div class='details'>
                <div class='detail-row'>
                    <span class='detail-label'>Status:</span>
                    <span class='detail-value'><span class='status-badge'>{statusFormatted}</span></span>
                </div>
                <div class='detail-row'>
                    <span class='detail-label'>Scheduled Date:</span>
                    <span class='detail-value'>{dateFormatted}</span>
                </div>
                <div class='detail-row'>
                    <span class='detail-label'>Time:</span>
                    <span class='detail-value'>{timeFormatted}</span>
                </div>
            </div>
            
            <p>If you have any questions or concerns regarding your viewing request, please don't hesitate to contact us.</p>
            
            <p style='margin-top: 30px;'>Best regards,<br><strong>The DubiRent Team</strong></p>
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

