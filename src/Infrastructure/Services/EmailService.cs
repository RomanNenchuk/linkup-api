using Application.Common;
using Application.Common.Interfaces;
using Application.Common.Options;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

namespace Infrastructure.Services;

public class EmailService(IOptions<SmtpSettings> settings, ILogger<EmailService> logger) : IEmailService
{
    private readonly SmtpSettings _settings = settings.Value;
    private readonly ILogger<EmailService> _logger = logger;

    public async Task<Result<string>> SendEmailAsync(string to, string subject, string body, bool isHtml = true)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_settings.SenderName, _settings.SenderEmail));
        message.To.Add(MailboxAddress.Parse(to));
        message.Subject = subject;

        if (isHtml)
            message.Body = new TextPart("html") { Text = body };
        else
            message.Body = new TextPart("plain") { Text = body };

        using var client = new SmtpClient();
        try
        {
            await client.ConnectAsync(_settings.Server, _settings.Port, _settings.UseSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.Auto);
            await client.AuthenticateAsync(_settings.Username, _settings.Password);

            await client.SendAsync(message);
            _logger.LogInformation("Email sent to {Recipient}", to);
            return Result<string>.Success(to);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending email");
            return Result<string>.Failure($"Failed to send verification email to {to}", 400);
        }
        finally
        {
            await client.DisconnectAsync(true);
        }
    }
}
