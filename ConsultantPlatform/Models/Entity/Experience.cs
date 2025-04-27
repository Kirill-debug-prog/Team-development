using System;
using System.Collections.Generic;

namespace ConsultantPlatform.Models.Entity;

public partial class Experience
{
    public Guid Id { get; set; }

    public Guid MentorCardId { get; set; }

    public string CompanyName { get; set; } = null!;

    public string Position { get; set; } = null!;

    public float DurationYears { get; set; }

    public string? Description { get; set; }

    public virtual MentorCard MentorCard { get; set; } = null!;
}
