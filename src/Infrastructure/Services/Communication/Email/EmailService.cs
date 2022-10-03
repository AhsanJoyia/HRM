using Application.Configurations.MailJet;
using Application.Configurations.SMTP;
using Application.Contracts.Infrastructure;
using Application.Models.EmailModels;
using Mailjet.Client;
using Mailjet.Client.TransactionalEmails;
using Mailjet.Client.TransactionalEmails.Response;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using MimeKit.Text;

namespace Infrastructure.Services.Communication.Email;

internal class EmailService : IEmailService
{
    private readonly MailJetConfig _mailJetConfig;
    private readonly SMTPConfig _smptConfig;

    public EmailService(IOptions<SMTPConfig> smptConfig, IOptions<MailJetConfig> mailJetConfig)
    {
        _mailJetConfig = mailJetConfig.Value;
        _smptConfig = smptConfig.Value;
    }
    #region SMTP
    public async Task<string> SendEmailbySMTPAsync(SMTPEmailRequest smtpEmailRequest)
    {
        try
        {
            MimeMessage email = new MimeMessage();

            email.From.Add(MailboxAddress.Parse(_smptConfig.EmailFrom));
            email.To.Add(MailboxAddress.Parse(smtpEmailRequest.ToEmail));
            email.Subject = smtpEmailRequest.EmailSubject;
            email.Body = new TextPart(TextFormat.Html) { Text = smtpEmailRequest.EmailBody };

            var builder = new BodyBuilder();
            byte[] fileBytes;

            if (smtpEmailRequest.Attachments != null)
            {
                var files = smtpEmailRequest.Attachments;
                if (files.Count > 0)
                {
                    foreach (var file in files)
                    {
                        using (var ms = new MemoryStream())
                        {
                            file.CopyTo(ms);
                            fileBytes = ms.ToArray();
                        }
                        builder.Attachments.Add(file.FileName, fileBytes, ContentType.Parse(file.ContentType));
                        //builder.Attachments.Add(file.FileName);
                    }
                }
            }
            string emailResult = "";
            using (var smtp = new MailKit.Net.Smtp.SmtpClient())
            {
                smtp.Connect(_smptConfig.SMTPHost, _smptConfig.SMTPPort, SecureSocketOptions.StartTls);

                // Uncommit this line if you want to add authenticaiton. It is always recommended
                smtp.Authenticate(_smptConfig.AccountEmail, _smptConfig.AccountPassword);
                emailResult = await smtp.SendAsync(email);
                smtp.Disconnect(true);
                await smtp.DisconnectAsync(true);
            }

            return emailResult;
        }
        catch (Exception ex)
        {
            throw new Exception($"Unable to send email {ex.Message}");
        }
    }


    #endregion

    #region MailJet
    public async Task<TransactionalEmailResponse> SendEmailByMailJetAsync(MailJetEmailRequest mailJetEmailRequest)
    {
        MailjetClient _client = new MailjetClient(_mailJetConfig.ApiKey, _mailJetConfig.SecretKey);

        var email = new TransactionalEmailBuilder()
               .WithFrom(new SendContact(!string.IsNullOrEmpty(mailJetEmailRequest.FromEmail) ? mailJetEmailRequest.FromEmail : _mailJetConfig.SenderEmail))
               .WithSubject(mailJetEmailRequest.EmailSubject)
               .WithHtmlPart(mailJetEmailRequest.EmailBody)
               .WithTo(new SendContact(mailJetEmailRequest.ToEmail))
               .Build();
        return await _client.SendTransactionalEmailAsync(email);
    }
    #endregion
}
