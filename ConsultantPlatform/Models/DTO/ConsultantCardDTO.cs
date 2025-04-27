using System;
using System.Collections.Generic; // Для List<>
using System.ComponentModel.DataAnnotations; // Если будете добавлять валидацию сюда

namespace ConsultantPlatform.Models.DTO
{
    public class ConsultantCardDTO
    {
        public Guid Id { get; set; }

        // --- Добавляем валидацию для полей, которые были пропущены ранее ---
        [Required(ErrorMessage = "Title is required")]
        [StringLength(200, MinimumLength = 5, ErrorMessage = "Title must be between 5 and 200 characters")]
        public string Title { get; set; } = null!;

        public string? Description { get; set; }

        // MentorId здесь информационный, для обновления/создания он берется из токена
        public Guid MentorId { get; set; }

        [Required(ErrorMessage = "Price per hour is required")]
        [Range(0, (double)decimal.MaxValue, ErrorMessage = "Price must be non-negative")] // Убедимся, что цена не отрицательная
        public decimal PricePerHours { get; set; }

        // --- Заменяем старое поле Experience на коллекцию ---
        public List<ExperienceDTO> Experiences { get; set; } = new List<ExperienceDTO>();

        // Опционально: можно добавить поле для суммарного опыта, если нужно
        // public float? TotalExperienceYears { get; set; }

        // --- Добавим имя Ментора для удобства отображения? ---
        // public string? MentorFullName { get; set; } // Потребует Include(c => c.Mentor) в сервисе
    }
}