using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ConsultantPlatform.Models.DTO
{
    public class ConsultantCardDTO
    {
        public Guid Id { get; set; }

        [Required(ErrorMessage = "Заголовок карточки обязателен")]
        [StringLength(200, MinimumLength = 5, ErrorMessage = "Заголовок должен содержать от {2} до {1} символов")]
        public string Title { get; set; } = null!;

        public string? Description { get; set; }

        public Guid MentorId { get; set; }

        public string? MentorFullName { get; set; }

        [Required(ErrorMessage = "Цена за час обязательна")]
        [Range(0, (double)decimal.MaxValue, ErrorMessage = "Цена должна быть неотрицательной")]
        public decimal PricePerHours { get; set; }

        public List<ExperienceDTO> Experiences { get; set; } = new List<ExperienceDTO>();

    }
}