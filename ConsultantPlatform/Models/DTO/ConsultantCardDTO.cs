using ConsultantPlatform.Models.Entity;
using System.ComponentModel.DataAnnotations;

namespace ConsultantPlatform.Models.DTO
{
    public class ConsultantCardDTO
    {
        public Guid Id { get; set; }

        public string Title { get; set; } = null!;

        public string? Description { get; set; }

        public Guid MentorId { get; set; }

        public decimal PricePerHours { get; set; }

        public List<Experience> Experience { get; set; }

    }
}
