using System.ComponentModel.DataAnnotations;

namespace ConsultantPlatform.Models.DTO
{
    /// <summary>
    /// Data Transfer Object for updating user profile information.
    /// </summary>
    public class UpdateUserProfileDTO
    {
        // Не включаем Login, так как его изменение сложнее
        // Не включаем Id, так как ID пользователя берется из токена/маршрута

        [Required(ErrorMessage = "First name is required")] // Делаем обязательными при обновлении
        [StringLength(50, MinimumLength = 2, ErrorMessage = "First name must be between 2 and 50 characters")]
        [RegularExpression(@"^[a-zA-Zа-яА-ЯёЁ\s-]+$", ErrorMessage = "First name can only contain letters (Latin and Cyrillic), spaces, and hyphens")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Last name is required")] // Делаем обязательными при обновлении
        [StringLength(50, MinimumLength = 2, ErrorMessage = "Last name must be between 2 and 50 characters")]
        [RegularExpression(@"^[a-zA-Zа-яА-ЯёЁ\s-]+$", ErrorMessage = "Last name can only contain letters (Latin and Cyrillic), spaces, and hyphens")]
        public string LastName { get; set; } = string.Empty;

        [StringLength(50, ErrorMessage = "Middle name cannot exceed 50 characters")]
        [RegularExpression(@"^[a-zA-Zа-яА-ЯёЁ\s-]*$", ErrorMessage = "Middle name can only contain letters (Latin and Cyrillic), spaces, and hyphens")]
        public string? MiddleName { get; set; } // Отчество остается необязательным
    }
}