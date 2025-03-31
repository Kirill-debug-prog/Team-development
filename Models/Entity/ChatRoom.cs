using System;
using System.Collections.Generic;

namespace ConsultantPlatform.Models.Entity;

public partial class ChatRoom
{
    public Guid Id { get; set; }

    public string? Title { get; set; }

    public Guid MentorId { get; set; }

    public Guid ClientId { get; set; }

    public virtual User Client { get; set; } = null!;

    public virtual User Mentor { get; set; } = null!;

    public virtual ICollection<Message> Messages { get; set; } = new List<Message>();
}
