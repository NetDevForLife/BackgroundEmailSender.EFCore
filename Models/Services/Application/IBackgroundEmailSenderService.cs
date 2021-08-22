using System.Threading;
using System.Threading.Tasks;
using BackgroundEmailSenderSample.Models.Entities;
using BackgroundEmailSenderSample.Models.ViewModels;

namespace BackgroundEmailSenderSample.Models.Services.Application
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