using System;
using System.Threading;
using System.Threading.Tasks;
using background_email_sender_master.Models.Entities;
using BackgroundEmailSenderSample.Models.Options;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

namespace background_email_sender_master.Models.Services.Application
{
    public class BackgroundEmailSenderService : IBackgroundEmailSenderService
    {
        private readonly IOptionsMonitor<SmtpOptions> smtpOptionsMonitor;
        private readonly ILogger<BackgroundEmailSenderService> logger;
        
        public BackgroundEmailSenderService(IOptionsMonitor<SmtpOptions> smtpOptionsMonitor, ILogger<BackgroundEmailSenderService> logger)
        {
            this.logger = logger;
            this.smtpOptionsMonitor = smtpOptionsMonitor;
        }

        public async Task SendEmailAsync(Email model, CancellationToken token)
        {
            try
            {
                var options = this.smtpOptionsMonitor.CurrentValue;
                
                using SmtpClient client = new SmtpClient();
                
                await client.ConnectAsync(options.Host, options.Port, options.Security);
                
                if (!string.IsNullOrEmpty(options.Username))
                {
                    await client.AuthenticateAsync(options.Username, options.Password);
                }

                MimeMessage message = new MimeMessage();

                message.From.Add(MailboxAddress.Parse(options.Sender));
                message.To.Add(MailboxAddress.Parse(model.Recipient));
                message.Subject = model.Subject;

                var builder = new BodyBuilder();

                // if (model.attachments != null)
                // {
                //     byte[] fileBytes;
                //     foreach (var file in model.attachments)
                //     {
                //         if (file.Length > 0)
                //         {
                //             using (var ms = new MemoryStream())
                //             {
                //                 file.CopyTo(ms);
                //                 fileBytes = ms.ToArray();
                //             }

                //             builder.Attachments.Add(file.FileName, fileBytes, ContentType.Parse(file.ContentType));
                //         }
                //     }
                // }

                builder.HtmlBody = model.Message;
                message.Body = builder.ToMessageBody();

                await client.SendAsync(message);
                await client.DisconnectAsync(true);
            }
            catch
            {
                throw new Exception();
            }
        }

        public Task SaveEmailAsync(Email input, CancellationToken token)
        {
            //Salvataggio della mail su database

            // int affectedRows = await db.CommandAsync($@"INSERT INTO EmailMessages (Id, Recipient, Subject, Message, SenderCount, Status) 
            //                                             VALUES ({message.MessageId}, {email}, {subject}, {htmlMessage}, 0, {nameof(MailStatus.InProgress)})");

            // if (affectedRows != 1)
            // {
            //     throw new InvalidOperationException($"Could not persist email message to {email}");
            // }
            
            throw new Exception();
        }

        public Task DeleteEmailAsync(Email input, CancellationToken token)
        {
            throw new Exception();
        }
    }
}