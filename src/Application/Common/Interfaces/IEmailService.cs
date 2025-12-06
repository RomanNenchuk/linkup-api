namespace Application.Common.Interfaces;

public interface IEmailService
{
    Task<Result<string>> SendEmailAsync(string to, string subject, string body, bool isHtml = true);
    string BuildEmailTemplate(string title, string message, string actionUrl, string actionText, string footerNote = default!);
}
