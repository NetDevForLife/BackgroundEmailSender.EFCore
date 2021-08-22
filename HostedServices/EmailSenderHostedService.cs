using System;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using background_email_sender_master.Models.Entities;
using background_email_sender_master.Models.Services.Application;
using background_email_sender_master.Models.ViewModels;
using BackgroundEmailSenderSample.Models.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using SequentialGuid;

namespace BackgroundEmailSenderSample.HostedServices
{
    public class EmailSenderHostedService : IHostedService, IDisposable
    {
        private readonly IBackgroundEmailSenderService backgroundEmailSenderService;
        private readonly IOptionsMonitor<SmtpOptions> optionsMonitor;
        private readonly BufferBlock<MimeMessage> mailMessages;
        private readonly ILogger logger;

        private CancellationToken token;
        private CancellationTokenSource deliveryCancellationTokenSource;
        private Task deliveryTask;

        public EmailSenderHostedService(IConfiguration configuration, IBackgroundEmailSenderService backgroundEmailSenderService, 
                                        ILogger<EmailSenderHostedService> logger, IOptionsMonitor<SmtpOptions> optionsMonitor, CancellationToken token)
        {
            this.token = token;
            this.logger = logger;
            this.optionsMonitor = optionsMonitor;
            this.mailMessages = new BufferBlock<MimeMessage>();
            this.backgroundEmailSenderService = backgroundEmailSenderService;
        }

        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            Email message = new();

            message.Id = message.Id ?? SequentialGuidGenerator.Instance.NewGuid().ToString();
            message.Recipient = email;
            message.Subject = subject;
            message.Message = htmlMessage;

            await backgroundEmailSenderService.SaveEmailAsync(message, token);

            await backgroundEmailSenderService.SendEmailAsync(message, token);
        }

        public async Task StartAsync(CancellationToken token)
        {
            logger.LogInformation("Starting background e-mail delivery");

            ListViewModel<EmailViewModel> email = await backgroundEmailSenderService.FindEmailAsync();

            try
            {
                foreach (EmailViewModel riga in email.Results)
                {
                    Email message = new();

                    message.Recipient = riga.Message;
                    message.Subject = riga.Subject;
                    message.Message = riga.Message;
                    message.Id = riga.Id;

                    await backgroundEmailSenderService.SendEmailAsync(message, token);
                }

                logger.LogInformation("Email delivery started: {count} message(s) were resumed for delivery", email.TotalCount);

                deliveryCancellationTokenSource = new CancellationTokenSource();
                deliveryTask = DeliverAsync(deliveryCancellationTokenSource.Token);
            }
            catch (Exception startException)
            {
                logger.LogError(startException, "Couldn't start email delivery");
            }
        }

        public async Task StopAsync(CancellationToken token)
        {
            CancelDeliveryTask();
            await Task.WhenAny(deliveryTask, Task.Delay(Timeout.Infinite, token));
        }

        private void CancelDeliveryTask()
        {
            try
            {
                if (deliveryCancellationTokenSource != null)
                {
                    logger.LogInformation("Stopping e-mail background delivery");
                    deliveryCancellationTokenSource.Cancel();
                    deliveryCancellationTokenSource = null;
                }
            }
            catch
            {
            }
        }

        public async Task DeliverAsync(CancellationToken token)
        {
            logger.LogInformation("E-mail background delivery started");
            while (!token.IsCancellationRequested)
            {
                MimeMessage message = null;
                Email msg = new();
                
                try
                {
                    message = await mailMessages.ReceiveAsync(token);

                    msg.Id = message.MessageId;
                    msg.Recipient = message.To.ToString();
                    msg.Subject = msg.Subject;
                    msg.Message = msg.Message;

                    await backgroundEmailSenderService.SendEmailAsync(msg, token);
                    await backgroundEmailSenderService.UpdateEmailAsync(msg, token);
                    
                    logger.LogInformation($"E-mail sent successfully to {message.To}");
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception sendException)
                {
                    var recipient = message?.To[0];
                    logger.LogError(sendException, "Couldn't send an e-mail to {recipient}", recipient);

                    try
                    {
                        //bool shouldRequeue = await db.QueryScalarAsync<bool>($"UPDATE EmailMessages SET SenderCount = SenderCount + 1, Status=CASE WHEN SenderCount < {optionsMonitor.CurrentValue.MaxSenderCount} THEN Status ELSE {nameof(MailStatus.Deleted)} END WHERE Id={message.MessageId}; SELECT COUNT(*) FROM EmailMessages WHERE Id={message.MessageId} AND Status NOT IN ({nameof(MailStatus.Deleted)}, {nameof(MailStatus.Sent)})", token);
                        bool shouldRequeue = false;

                        if (shouldRequeue)
                        {
                            await backgroundEmailSenderService.SendEmailAsync(msg, token);
                        }
                    }
                    catch (Exception requeueException)
                    {
                        logger.LogError(requeueException, "Couldn't requeue message to {0}", recipient);
                    }

                    await Task.Delay(optionsMonitor.CurrentValue.DelayOnError, token);
                }
            }

            logger.LogInformation("E-mail background delivery stopped");
        }

        public void Dispose()
        {
            CancelDeliveryTask();
        }
    }
}