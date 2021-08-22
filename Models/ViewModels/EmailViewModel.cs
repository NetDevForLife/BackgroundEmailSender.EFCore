using background_email_sender_master.Models.Entities;

namespace background_email_sender_master.Models.ViewModels
{
    public class EmailViewModel
    {
        public string Id { get; set; }
        public string Recipient { get; set; }
        public string Subject { get; set; }
        public string Message { get; set; }
        public int SenderCount { get; set; }
        public string Status { get; set; }

        public static EmailViewModel FromEntity(Email email)
        {
            return new EmailViewModel {
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