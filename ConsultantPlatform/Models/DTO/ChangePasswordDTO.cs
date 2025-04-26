using System.ComponentModel.DataAnnotations;

namespace ConsultantPlatform.Models.DTO
{
    /// <summary>
    /// Data Transfer Object for changing the user's password.
    /// </summary>
    public class ChangePasswordDTO
    {
        [Required(ErrorMessage = "Current password is required")]
        [DataType(DataType.Password)]
        public string OldPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "New password is required")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "New password must be between 8 and 100 characters")]
        [DataType(DataType.Password)]
        public string NewPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Confirm New Password is required")]
        [DataType(DataType.Password)]
        [Compare("NewPassword", ErrorMessage = "New password and confirmation password do not match.")]
        public string ConfirmNewPassword { get; set; } = string.Empty;
    }
}