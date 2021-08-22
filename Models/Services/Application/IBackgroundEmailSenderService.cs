using System.Threading;
using System.Threading.Tasks;
using background_email_sender_master.Models.Entities;
using background_email_sender_master.Models.ViewModels;

namespace background_email_sender_master.Models.Services.Application
{
    public interface IBackgroundEmailSenderService
    {
        Task<ListViewModel<EmailViewModel>> FindEmailAsync();
        Task<EmailDetailViewModel> FindMessageAsync(Email model, CancellationToken token);
        
        Task SendEmailAsync(Email model, CancellationToken token);
        
        Task SaveEmailAsync(Email model, CancellationToken token);

        Task UpdateEmailAsync(Email model, CancellationToken token);
        Task UpdateStatusAsync(Email model, CancellationToken token);
        Task UpdateCounterAsync(Email model, CancellationToken token);
    }
}