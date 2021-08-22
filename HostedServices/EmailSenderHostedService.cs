using System;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using background_email_sender_master.Models.Entities;
using background_email_sender_master.Models.Services.Application;
using BackgroundEmailSenderSample.Models.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

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
            Email message = new Email();

            message.Id = Guid.NewGuid().ToString();
            message.Recipient = email;
            message.Subject = subject;
            message.Message = htmlMessage;

            //SALTAGGIO EMAIL NEL DATABASE
            await backgroundEmailSenderService.SaveEmailAsync(message, token);

            //INVIO EMAIL
            await backgroundEmailSenderService.SendEmailAsync(message, token);
        }

        public async Task StartAsync(CancellationToken token)
        {
            logger.LogInformation("Starting background e-mail delivery");

            // FormattableString query = $@"SELECT Id, Recipient, Subject, Message FROM EmailMessages WHERE Status NOT IN ({nameof(MailStatus.Sent)}, {nameof(MailStatus.Deleted)})";
            // DataSet dataSet = await db.QueryAsync(query);

            try
            {
            //     foreach (DataRow row in dataSet.Tables[0].Rows)
            //     {
            //         var message = CreateMessage(Convert.ToString(row["Recipient"]),
            //                                     Convert.ToString(row["Subject"]),
            //                                     Convert.ToString(row["Message"]),
            //                                     Convert.ToString(row["Id"]));

            //         await this.mailMessages.SendAsync(message, token);
            //     }

            //     logger.LogInformation("Email delivery started: {count} message(s) were resumed for delivery", dataSet.Tables[0].Rows.Count);

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
                Email msg = new Email();
                
                try
                {
                    // message = await mailMessages.ReceiveAsync(token);

                    // var options = this.optionsMonitor.CurrentValue;
                    // using var client = new SmtpClient();

                    // await client.ConnectAsync(options.Host, options.Port, options.Security, token);
                    // if (!string.IsNullOrEmpty(options.Username))
                    // {
                    //     await client.AuthenticateAsync(options.Username, options.Password, token);
                    // }

                    // await client.SendAsync(message, token);
                    // await client.DisconnectAsync(true, token);

                    // //await db.CommandAsync($"UPDATE EmailMessages SET Status={nameof(MailStatus.Sent)} WHERE Id={message.MessageId}", token);
                    // logger.LogInformation($"E-mail sent successfully to {message.To}");

                    message = await mailMessages.ReceiveAsync(token);

                    msg.Id = message.MessageId;
                    msg.Recipient = message.To.ToString();
                    msg.Subject = msg.Subject;
                    msg.Message = msg.Message;

                    await backgroundEmailSenderService.SendEmailAsync(msg, token);
                    
                    //await db.CommandAsync($"UPDATE EmailMessages SET Status={nameof(MailStatus.Sent)} WHERE Id={message.MessageId}", token);
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