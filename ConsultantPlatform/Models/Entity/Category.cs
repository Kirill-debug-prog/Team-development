using System;
using System.Collections.Generic;

namespace ConsultantPlatform.Models.Entity;

public partial class Category
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public virtual ICollection<MentorCardsCategory> MentorCardsCategories { get; set; } = new List<MentorCardsCategory>();
}
