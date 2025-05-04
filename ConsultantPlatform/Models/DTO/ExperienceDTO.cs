using System;
using System.ComponentModel.DataAnnotations;

namespace ConsultantPlatform.Models.DTO
{

    public class ExperienceDTO
    {
        public Guid Id { get; set; }

        [Required(ErrorMessage = "Название компании обязательно.")]
        [StringLength(255, ErrorMessage = "Название компании не может превышать {1} символов.")]
        public string CompanyName { get; set; } = null!;

        [Required(ErrorMessage = "Должность обязательна.")]
        [StringLength(255, ErrorMessage = "Должность не может превышать {1} символов.")]
        public string Position { get; set; } = null!;

        [Required(ErrorMessage = "Длительность опыта в годах обязательна.")]
        [Range(0, 100, ErrorMessage = "Длительность опыта должна быть от {1} до {2} лет.")]
        public float DurationYears { get; set; }

        [StringLength(1000, ErrorMessage = "Описание не может превышать {1} символов.")]
        public string? Description { get; set; }

    }
}