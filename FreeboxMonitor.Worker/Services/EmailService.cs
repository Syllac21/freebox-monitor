using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using Microsoft.Extensions.Options;
using FreeboxMonitor.Worker.Models;

namespace FreeboxMonitor.Worker.Services;

public class EmailService(IOptions<EmailOptions> emailOptions, ILogger<EmailService> logger)
{
    private readonly EmailOptions _options = emailOptions.Value;

    public async Task SendReportAsync(string subject, string htmlBody)
    {
        var message = new MimeMessage();
        message.From.Add(MailboxAddress.Parse(_options.FromAddress));
        message.To.Add(MailboxAddress.Parse(_options.ToAddress));
        message.Subject = subject;

        message.Body = new TextPart("html")
        {
            Text = htmlBody
        };

        using var client = new SmtpClient();

        try
        {
            await client.ConnectAsync(_options.SmtpHost, _options.SmtpPort, SecureSocketOptions.SslOnConnect);
            await client.AuthenticateAsync(_options.Username, _options.Password);
            await client.SendAsync(message);
            logger.LogInformation("Rapport envoyé par email à {To}", _options.ToAddress);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Échec de l'envoi du rapport par email");
        }
        finally
        {
            await client.DisconnectAsync(true);
        }
    }
}
