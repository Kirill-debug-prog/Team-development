using ConsultantPlatform.Models.DTO;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ConsultantPlatform.WebApp.Models.DTOs
{
    public class CreateMentorCardDTO
    {
        [Required(ErrorMessage = "Заголовок карточки обязателен")]
        [StringLength(200, MinimumLength = 5, ErrorMessage = "Заголовок должен содержать от {2} до {1} символов")]
        public string Title { get; set; } = null!;

        public string? Description { get; set; }

        [Required(ErrorMessage = "ID ментора обязателен")]
        public Guid MentorId { get; set; } // ID ментора, к которому привязывается карточка

        [Required(ErrorMessage = "Цена за час обязательна")]
        [Range(0, (double)decimal.MaxValue, ErrorMessage = "Цена должна быть неотрицательной")]
        public decimal PricePerHours { get; set; }

        // Список ID выбранных категорий
        public List<int>? SelectedCategoryIds { get; set; } = new List<int>();

        // Список опыта для добавления (опционально)
        public List<ExperienceDTO>? Experiences { get; set; } = new List<ExperienceDTO>();
    }
}