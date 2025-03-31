using System.ComponentModel.DataAnnotations;

namespace ConsultantPlatform.Models.DTO
{
    public class LoginDTO
    {
        [Required(ErrorMessage = "Login is required")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "Login must be between 3 and 50 characters")]
        [RegularExpression(@"^[a-zA-Z0-9_@.-]+$",
            ErrorMessage = "Login can only contain letters, numbers, and the following characters: _ @ . -")]
        public string Login { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be between 8 and 100 characters")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

    }
}
