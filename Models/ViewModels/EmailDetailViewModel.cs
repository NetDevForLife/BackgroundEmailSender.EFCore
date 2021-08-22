using BackgroundEmailSenderSample.Models.Entities;

namespace BackgroundEmailSenderSample.Models.ViewModels
{
    public class EmailDetailViewModel
    {
        public string Id { get; set; }
        public string Recipient { get; set; }
        public string Subject { get; set; }
        public string Message { get; set; }
        public int SenderCount { get; set; }
        public string Status { get; set; }
        
        public static EmailDetailViewModel FromEntity(Email email)
        {
            return new EmailDetailViewModel {
                Id = email.Id,
                Recipient = email.Recipient,
                Subject = email.Subject,
                Message = email.Message,
                SenderCount = email.SenderCount,
                Status = email.Status
            };
        }
    }
}