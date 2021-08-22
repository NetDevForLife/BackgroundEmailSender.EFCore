using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using background_email_sender_master.Models.Entities;
using background_email_sender_master.Models.Enums;
using background_email_sender_master.Models.ViewModels;
using BackgroundEmailSenderSample.Models.Options;
using BackgroundEmailSenderSample.Models.Services.Infrastructure;
using MailKit.Net.Smtp;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

namespace background_email_sender_master.Models.Services.Application
{
    public class BackgroundEmailSenderService : IBackgroundEmailSenderService
    {
        private readonly IOptionsMonitor<SmtpOptions> smtpOptionsMonitor;
        private readonly ILogger<BackgroundEmailSenderService> logger;
        private readonly MyEmailSenderDbContext dbContext;
        
        public BackgroundEmailSenderService(IOptionsMonitor<SmtpOptions> smtpOptionsMonitor, ILogger<BackgroundEmailSenderService> logger,
                                            MyEmailSenderDbContext dbContext)
        {
            this.logger = logger;
            this.dbContext = dbContext;
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

        public async Task SaveEmailAsync(Email input, CancellationToken token)
        {
            dbContext.Add(input);

            try
            {
                await dbContext.SaveChangesAsync();
            }
            catch (DbUpdateException exc) when ((exc.InnerException as SqliteException)?.SqliteErrorCode == 19)
            {
                throw new Exception();
            }
        }

        public async Task UpdateEmailAsync(Email model, CancellationToken token)
        {
            Email email = await dbContext.Emails.FindAsync(model.Id);

            email.ChangeStatus(nameof(MailStatus.Sent));

            try
            {
                await dbContext.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                throw new Exception();
            }
        }

        public async Task UpdateStatusAsync(Email model, CancellationToken token)
        {
            Email email = await dbContext.Emails.FindAsync(model.Id);

            email.ChangeStatus(nameof(MailStatus.Deleted));

            try
            {
                await dbContext.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                throw new Exception();
            }
        }

        public async Task UpdateCounterAsync(Email model, CancellationToken token)
        {
            int counter = 0;
            int newCounter = 0;

            Email email = await dbContext.Emails.FindAsync(model.Id);

            counter = model.SenderCount;
            newCounter = counter + 1;

            email.ChangeSenderCount(newCounter);

            try
            {
                await dbContext.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                throw new Exception();
            }
        }

        public async Task<ListViewModel<EmailViewModel>> FindEmailAsync()
        {
            IQueryable<Email> baseQuery = dbContext.Emails;

            IQueryable<Email> queryLinq = baseQuery
                .Where(email => email.Status != nameof(MailStatus.Sent) || email.Status != nameof(MailStatus.Sent))
                .AsNoTracking();

            List<EmailViewModel> emails = await queryLinq
                .Select(email => EmailViewModel.FromEntity(email))
                .ToListAsync();
            
            int totalCount = await queryLinq.CountAsync();

            ListViewModel<EmailViewModel> result = new()
            {
                Results = emails,
                TotalCount = totalCount
            };

            return result;
        }

        public async Task<EmailDetailViewModel> FindMessageAsync(Email model, CancellationToken token)
        {
            IQueryable<EmailDetailViewModel> queryLinq = dbContext.Emails
                .AsNoTracking()
                .Where(email => email.Id == model.Id)
                .Select(email => EmailDetailViewModel.FromEntity(email));

            EmailDetailViewModel viewModel = await queryLinq.FirstOrDefaultAsync();

            if (viewModel == null)
            {
                throw new Exception();
            }

            return viewModel;
        }
    }
}