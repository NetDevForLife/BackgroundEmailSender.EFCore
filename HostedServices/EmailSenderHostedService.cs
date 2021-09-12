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
using Microsoft.Extensions.DependencyInjection;

namespace BackgroundEmailSenderSample.HostedServices
{
    public class EmailSenderHostedService : IHostedService, IDisposable
    {
        private readonly BufferBlock<MimeMessage> mailMessages;
        private readonly IServiceScopeFactory serviceScopeFactory;
        private readonly ILogger logger;
        private readonly IOptionsMonitor<SmtpOptions> optionsMonitor;

        private CancellationTokenSource deliveryCancellationTokenSource;
        private Task deliveryTask;

        public EmailSenderHostedService(IConfiguration configuration, IServiceScopeFactory serviceScopeFactory, IOptionsMonitor<SmtpOptions> optionsMonitor, ILogger<EmailSenderHostedService> logger)
        {
            this.optionsMonitor = optionsMonitor;
            this.serviceScopeFactory = serviceScopeFactory;
            this.logger = logger;
            this.mailMessages = new BufferBlock<MimeMessage>();
        }

        public async Task SendEmailAsync(Email model)
        {
            using var serviceScope = serviceScopeFactory.CreateScope();
            var backgroundEmailSenderService = serviceScope.ServiceProvider.GetRequiredService<IBackgroundEmailSenderService>();

            await backgroundEmailSenderService.SaveEmailAsync(model);
            await backgroundEmailSenderService.SendEmailAsync(model);
        }

        public async Task StartAsync(CancellationToken token)
        {
            logger.LogInformation("Starting background e-mail delivery");

            using var serviceScope = serviceScopeFactory.CreateScope();
            var backgroundEmailSenderService = serviceScope.ServiceProvider.GetRequiredService<IBackgroundEmailSenderService>();

            ListViewModel<EmailViewModel> email = await backgroundEmailSenderService.FindEmailAsync();

            try
            {
                foreach (EmailViewModel riga in email.Results)
                {
                    Email message = new();

                    message.Recipient = riga.Recipient;
                    message.Subject = riga.Subject;
                    message.Message = riga.Message;
                    message.Id = riga.Id;

                    await backgroundEmailSenderService.SendEmailAsync(message);
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

            using var serviceScope = serviceScopeFactory.CreateScope();
            var backgroundEmailSenderService = serviceScope.ServiceProvider.GetRequiredService<IBackgroundEmailSenderService>();

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

                    await backgroundEmailSenderService.SendEmailAsync(message);
                    await backgroundEmailSenderService.UpdateEmailAsync(message);
                    
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

                    EmailDetailViewModel viewModel = await backgroundEmailSenderService.FindMessageAsync(message);

                    int counter = 0;

                    if (viewModel != null)
                    {
                        try
                        {
                            counter = Convert.ToInt32(viewModel.SenderCount);

                            if (counter == 25)
                            {
                                await backgroundEmailSenderService.UpdateStatusAsync(message);
                            }
                            else
                            {
                                await backgroundEmailSenderService.UpdateCounterAsync(message);
                                await backgroundEmailSenderService.SendEmailAsync(message);
                            }
                        }
                        catch (Exception requeueException)
                        {
                            logger.LogError(requeueException, "Couldn't requeue message to {0}", recipient);
                        }
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