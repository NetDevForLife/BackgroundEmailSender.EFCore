using System.ComponentModel.DataAnnotations;

namespace BackgroundEmailSenderSample.Models.InputModels;

public class EmailInputModel
{
    [Required(ErrorMessage = "L'indirizzo email è obbligatorio"), EmailAddress, Display(Name = "Destinatario")]
    public string recipientEmail { get; set; }

    [Required(ErrorMessage = "L'oggetto è obbligatorio"), Display(Name = "Oggetto")]
    public string subject { get; set; }

    [Required(ErrorMessage = "Il messaggio è obbligatorio"), Display(Name = "Messaggio")]
    public string htmlMessage { get; set; }
}