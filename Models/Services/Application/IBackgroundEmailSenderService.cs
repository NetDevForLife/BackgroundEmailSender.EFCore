using System.Threading;
using System.Threading.Tasks;
using BackgroundEmailSenderSample.Models.Entities;
using BackgroundEmailSenderSample.Models.ViewModels;

namespace BackgroundEmailSenderSample.Models.Services.Application;

public interface IBackgroundEmailSenderService
{
    Task<ListViewModel<EmailViewModel>> FindEmailAsync();
    Task<EmailDetailViewModel> FindMessageAsync(Email model);

    Task SendEmailAsync(Email model);

    Task SaveEmailAsync(Email model);

    Task UpdateEmailAsync(Email model);
    Task UpdateStatusAsync(Email model);
    Task UpdateCounterAsync(Email model);
}