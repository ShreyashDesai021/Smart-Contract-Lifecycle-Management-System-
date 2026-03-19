using CLM.Core.Interfaces;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;

namespace CLM.Infrastructure.Services;

public class EmailService : IEmailService
{
    private readonly IConfiguration _config;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration config, ILogger<EmailService> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task SendExpiryReminderAsync(string toEmail, string contractTitle, int daysLeft)
    {
        var smtpEnabled = _config.GetValue<bool>("Email:Enabled");
        if (!smtpEnabled)
        {
            _logger.LogInformation("[Email Skipped] Would send expiry reminder to {Email} for '{Title}' ({Days} days left)",
                toEmail, contractTitle, daysLeft);
            return;
        }

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress("CLM System", _config["Email:From"]));
        message.To.Add(new MailboxAddress(toEmail, toEmail));
        message.Subject = $"⚠️ Contract Expiry Alert: {contractTitle}";
        message.Body = new TextPart("html")
        {
            Text = $@"<h2>Contract Expiry Reminder</h2>
                      <p>The contract <strong>{contractTitle}</strong> will expire in <strong>{daysLeft} days</strong>.</p>
                      <p>Please take necessary action in the CLM System.</p>"
        };

        using var smtp = new SmtpClient();
        await smtp.ConnectAsync(_config["Email:Host"], int.Parse(_config["Email:Port"] ?? "587"), false);
        await smtp.AuthenticateAsync(_config["Email:Username"], _config["Email:Password"]);
        await smtp.SendAsync(message);
        await smtp.DisconnectAsync(true);
    }
}
