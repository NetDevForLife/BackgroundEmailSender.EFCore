using System;
using System.Threading;
using System.Threading.Tasks;
using background_email_sender_master.Models.Entities;
using background_email_sender_master.Models.Services.Application;
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
        public async Task<IActionResult> SendMail(Email inputModel, [FromServices] IBackgroundEmailSenderService backgroundEmailSenderService, CancellationToken token) 
        {
            Email message = new Email();

            message.Id = Guid.NewGuid().ToString();
            message.Recipient = inputModel.Recipient;
            message.Subject = inputModel.Subject;
            message.Message = inputModel.Message;

            await backgroundEmailSenderService.SendEmailAsync(message, token);
            
            return RedirectToAction(nameof(ThankYou));
        }
        
        public IActionResult ThankYou() {
            return View();
        }

    }
}
