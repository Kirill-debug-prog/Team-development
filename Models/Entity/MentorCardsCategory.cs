using System;
using System.Collections.Generic;

namespace ConsultantPlatform.Models.Entity;

public partial class MentorCardsCategory
{
    public Guid Id { get; set; }

    public Guid MentorCardId { get; set; }

    public int CategoryId { get; set; }

    public virtual Category Category { get; set; } = null!;

    public virtual MentorCard MentorCard { get; set; } = null!;
}
