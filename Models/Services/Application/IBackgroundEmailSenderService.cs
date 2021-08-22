using System.Threading;
using System.Threading.Tasks;
using background_email_sender_master.Models.Entities;

namespace background_email_sender_master.Models.Services.Application
{
    public interface IBackgroundEmailSenderService
    {
        Task SendEmailAsync(Email model, CancellationToken token);
        Task SaveEmailAsync(Email model, CancellationToken token);
        Task DeleteEmailAsync(Email input, CancellationToken token);
    }
}