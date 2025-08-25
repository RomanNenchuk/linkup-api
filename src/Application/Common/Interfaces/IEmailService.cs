namespace Application.Common.Interfaces;

public interface IEmailService
{
    Task<Result<string>> SendEmailAsync(string to, string subject, string body, bool isHtml = true);
}
