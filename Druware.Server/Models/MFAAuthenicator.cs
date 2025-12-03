using System.ComponentModel.DataAnnotations;

namespace Druware.Server.Models;

public class MfaAuthenicator
{
    [Required(ErrorMessage = "Authenticator Code is required")]
    public string? Code { get; set; }
}