using System;
using System.Collections.Generic;

namespace ConsultantPlatform.Models.Entity;

public partial class User
{
    public Guid Id { get; set; }

    public string Login { get; set; } = null!;

    public string Password { get; set; } = null!;

    public string? FirstName { get; set; }

    public string? LastName { get; set; }

    public string? MiddleName { get; set; }

    public string? PhoneNumber { get; set; }

    public string? Email { get; set; }

    public virtual ICollection<ChatRoom> ChatRoomClients { get; set; } = new List<ChatRoom>();

    public virtual ICollection<ChatRoom> ChatRoomMentors { get; set; } = new List<ChatRoom>();

    public virtual ICollection<MentorCard> MentorCards { get; set; } = new List<MentorCard>();

    public virtual ICollection<Message> Messages { get; set; } = new List<Message>();
}
