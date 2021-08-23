using System;
using System.Threading;
using System.Threading.Tasks;
using BackgroundEmailSenderSample.Models.Entities;
using BackgroundEmailSenderSample.Models.Enums;
using BackgroundEmailSenderSample.Models.Services.Application;
using BackgroundEmailSenderSample.Models.InputModels;
using Microsoft.AspNetCore.Mvc;

namespace BackgroundEmailSenderSample.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> SendMail(EmailInputModel inputModel, [FromServices] IBackgroundEmailSenderService backgroundEmailSenderService) 
        {
            Email message = new Email();

            message.Id = Guid.NewGuid().ToString();
            message.Recipient = inputModel.recipientEmail;
            message.Subject = inputModel.subject;
            message.Message = inputModel.htmlMessage;
            message.SenderCount = 0;
            message.Status = nameof(MailStatus.InProgress);

            await backgroundEmailSenderService.SaveEmailAsync(message);
            
            return RedirectToAction(nameof(ThankYou));
        }
        
        public IActionResult ThankYou() {
            return View();
        }
    }
}
