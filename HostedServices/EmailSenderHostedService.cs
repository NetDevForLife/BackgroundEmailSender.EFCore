using System;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using BackgroundEmailSenderSample.Models.Entities;
using BackgroundEmailSenderSample.Models.Services.Application;
using BackgroundEmailSenderSample.Models.ViewModels;
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

        //public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        public async Task SendEmailAsync(Email model)
        {
            // Email message = new();

            // message.Id = message.Id ?? SequentialGuidGenerator.Instance.NewGuid().ToString();
            // message.Recipient = model.Recipient;
            // message.Subject = model.Subject;
            // message.Message = model.Message;

            await backgroundEmailSenderService.SaveEmailAsync(model, token);

            await backgroundEmailSenderService.SendEmailAsync(model, token);
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
                MimeMessage msg = null;
                Email message= new();

                try
                {
                    msg = await mailMessages.ReceiveAsync(token);

                    message.Id = msg.MessageId;
                    message.Recipient = msg.To.ToString();
                    message.Subject = msg.Subject;
                    message.Message = msg.TextBody;

                    await backgroundEmailSenderService.SendEmailAsync(message, token);
                    await backgroundEmailSenderService.UpdateEmailAsync(message, token);
                    
                    logger.LogInformation($"E-mail sent successfully to {msg.To}");
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception sendException)
                {
                    var recipient = msg?.To[0];
                    logger.LogError(sendException, "Couldn't send an e-mail to {recipient}", recipient);

                    EmailDetailViewModel viewModel = await backgroundEmailSenderService.FindMessageAsync(message, token);

                    int counter = 0;

                    if (viewModel != null)
                    try
                    {
                        counter = Convert.ToInt32(viewModel.SenderCount);

                        if (counter == 25)
                        {
                            await backgroundEmailSenderService.UpdateStatusAsync(message, token);
                        }
                        else
                        {
                            await backgroundEmailSenderService.UpdateCounterAsync(message, token);

                            await backgroundEmailSenderService.SendEmailAsync(message, token);
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