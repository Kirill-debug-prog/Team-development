using System;
using System.Collections.Generic;

namespace ConsultantPlatform.Models.Entity;

public partial class Message
{
    public Guid Id { get; set; }

    public Guid ChatRoomId { get; set; }

    public Guid SenderId { get; set; }

    public string Message1 { get; set; } = null!;

    public DateTime DateSent { get; set; }

    public virtual ChatRoom ChatRoom { get; set; } = null!;

    public virtual User Sender { get; set; } = null!;
}
