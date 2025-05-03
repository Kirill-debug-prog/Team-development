using System.ComponentModel.DataAnnotations;

namespace ConsultantPlatform.Models.DTO
{
    public class RegistrationDTO
    {
        [Required(ErrorMessage = "Login is required")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "Login must be between 3 and 50 characters")]
        [RegularExpression(@"^[a-zA-Z0-9_-]+$", ErrorMessage = "Login can only contain letters, numbers, underscores, and hyphens")]
        public string Login { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be between 8 and 100 characters")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Confirm Password is required")]
        [Compare("Password", ErrorMessage = "Password and Confirm Password must match")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "First name is required")]
        [StringLength(50, MinimumLength = 2, ErrorMessage = "First name must be between 2 and 50 characters")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Last name is required")]
        [StringLength(50, MinimumLength = 2, ErrorMessage = "Last name must be between 2 and 50 characters")]
        public string LastName { get; set; } = string.Empty;

        [StringLength(50, ErrorMessage = "Middle name cannot exceed 50 characters")]
        public string? MiddleName { get; set; }

        [Phone(ErrorMessage = "Неверный формат номера телефона.")]
        [StringLength(50, ErrorMessage = "Номер телефона не может превышать {1} символов.")]
        public string? PhoneNumber { get; set; }

        [EmailAddress(ErrorMessage = "Неверный формат адреса электронной почты.")]
        [StringLength(200, ErrorMessage = "Адрес электронной почты не может превышать {1} символов.")]
        public string? Email { get; set; }
    }
}