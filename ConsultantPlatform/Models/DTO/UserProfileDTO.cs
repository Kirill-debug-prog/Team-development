using System;

namespace ConsultantPlatform.Models.DTO
{
    /// <summary>
    /// Data Transfer Object for displaying user profile information.
    /// </summary>
    public class UserProfileDTO
    {
        /// <summary>
        /// User's unique identifier.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// User's login name.
        /// </summary>
        public string Login { get; set; } = string.Empty;

        /// <summary>
        /// User's first name.
        /// </summary>
        public string? FirstName { get; set; } // Может быть null, если не указано при регистрации

        /// <summary>
        /// User's last name.
        /// </summary>
        public string? LastName { get; set; } // Может быть null

        /// <summary>
        /// User's middle name (optional).
        /// </summary>
        public string? MiddleName { get; set; }
    }
}