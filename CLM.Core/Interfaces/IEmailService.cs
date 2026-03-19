namespace CLM.Core.Interfaces;

public interface IEmailService
{
    Task SendExpiryReminderAsync(string toEmail, string contractTitle, int daysLeft);
}
