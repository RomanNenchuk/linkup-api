using Application.Common;
using Application.Common.Interfaces;
using Application.Common.Options;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using System.Net;

namespace Infrastructure.Services;

public class EmailService(
    IOptions<SmtpOptions> settings,
    ILogger<EmailService> logger
) : IEmailService
{
    private readonly SmtpOptions _settings = settings.Value;
    private readonly ILogger<EmailService> _logger = logger;

    public async Task<Result<string>> SendEmailAsync(
        string to,
        string subject,
        string body,
        bool isHtml = true
    )
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_settings.SenderName, _settings.SenderEmail));
        message.To.Add(MailboxAddress.Parse(to));
        message.Subject = subject;

        message.Body = isHtml
            ? new TextPart("html") { Text = body }
            : new TextPart("plain") { Text = body };

        using var client = new SmtpClient();

        try
        {
            await client.ConnectAsync(
                _settings.Server,
                _settings.Port,
                _settings.UseSsl
                    ? SecureSocketOptions.StartTls
                    : SecureSocketOptions.Auto
            );

            await client.AuthenticateAsync(_settings.Username, _settings.Password);
            await client.SendAsync(message);

            _logger.LogInformation("Email successfully sent to {Recipient}", to);
            return Result<string>.Success(to);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {Recipient}", to);
            return Result<string>.Failure("Failed to send email.", 400);
        }
        finally
        {
            await client.DisconnectAsync(true);
        }
    }

    public string BuildEmailTemplate(
        string title,
        string message,
        string actionUrl,
        string actionText,
        string footerNote = "If you did not request this action, you can safely ignore this email."
    )
    {
        return $@"
        <!DOCTYPE html>
        <html lang=""en"">
        <head>
        <meta charset=""UTF-8"" />
        <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"" />
        <title>{WebUtility.HtmlEncode(title)}</title>
        </head>
        <body style=""margin:0; padding:0; background-color:#f6f7f9; font-family:Arial, sans-serif;"">
        <table width=""100%"" cellpadding=""0"" cellspacing=""0"">
            <tr>
            <td align=""center"" style=""padding:40px 20px;"">

                <table width=""100%"" style=""max-width:600px; background:#ffffff; border-radius:8px; padding:32px;"">
                <tr>
                    <td style=""font-size:22px; font-weight:bold; color:#333; padding-bottom:16px;"">
                    {WebUtility.HtmlEncode(title)}
                    </td>
                </tr>

                <tr>
                    <td style=""font-size:16px; color:#555; line-height:1.6; padding-bottom:24px;"">
                    {WebUtility.HtmlEncode(message)}
                    </td>
                </tr>

                <tr>
                    <td align=""center"" style=""padding-bottom:32px;"">
                    <a href=""{actionUrl}""
                        style=""display:inline-block;
                                padding:14px 28px;
                                background-color:#4f46e5;
                                color:#ffffff;
                                text-decoration:none;
                                border-radius:6px;
                                font-weight:bold;"">
                        {WebUtility.HtmlEncode(actionText)}
                    </a>
                    </td>
                </tr>

                <tr>
                    <td style=""font-size:14px; color:#888; line-height:1.5;"">
                    {WebUtility.HtmlEncode(footerNote)}
                    </td>
                </tr>
                </table>

                <div style=""font-size:12px; color:#aaa; margin-top:16px;"">
                © {DateTime.UtcNow.Year}
                </div>

            </td>
            </tr>
        </table>
        </body>
        </html>";
    }
}
