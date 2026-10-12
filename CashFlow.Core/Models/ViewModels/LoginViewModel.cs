using System.ComponentModel.DataAnnotations;

namespace CashFlow.Core.Models.ViewModels
{
    public class LoginViewModel
    {
        [Required]
        [Display(Name = "Username")]
        public string UserName { get; set; } = null!;

        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; } = null!;

        public bool HasPassword { get; set; }
    }
}
