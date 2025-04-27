using System;
using System.Collections.Generic;

namespace ConsultantPlatform.Models.Entity;

public partial class MentorCard
{
    public Guid Id { get; set; }

    public string Title { get; set; } = null!;

    public string? Description { get; set; }

    public Guid MentorId { get; set; }

    public decimal PricePerHours { get; set; }

    public virtual ICollection<Experience> Experiences { get; set; } = new List<Experience>();

    public virtual User Mentor { get; set; } = null!;

    public virtual ICollection<MentorCardsCategory> MentorCardsCategories { get; set; } = new List<MentorCardsCategory>();
}
