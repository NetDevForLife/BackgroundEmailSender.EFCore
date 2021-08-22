namespace BackgroundEmailSenderSample.Models.Entities
{
    public partial class Email
    {
        public string Id { get; set; }
        public string Recipient { get; set; }
        public string Subject { get; set; }
        public string Message { get; set; }
        public int SenderCount { get; set; }
        public string Status { get; set; }

        public void ChangeSenderCount(int newSenderCount)
        {
            SenderCount = newSenderCount;
        }

        public void ChangeStatus(string newStatus)
        {
            if (string.IsNullOrWhiteSpace(newStatus))
            {
                throw new System.Exception();
            }
            
            Status = newStatus;
        }
    }
}