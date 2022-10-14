using System;
using System.ComponentModel.DataAnnotations;

namespace Druware.Server.Models
{
    public class LoginModel
    {
        public LoginModel()
        {
        }

        [Required(ErrorMessage = "User Name is required")]
        [EmailAddress]
        public string? UserName { get; set; }
        [Required(ErrorMessage = "Password is required")]
        [DataType(DataType.Password)]
        public string? Password { get; set; }
    }
}

