using System;

namespace ConsultantPlatform.Models.DTO
{
    /// <summary>
    /// Data Transfer Object for displaying user profile information.
    /// </summary>
    public class UserProfileDTO
    {
        public Guid Id { get; set; }
        public string Login { get; set; } = string.Empty;
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? MiddleName { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Email { get; set; }
    }
}