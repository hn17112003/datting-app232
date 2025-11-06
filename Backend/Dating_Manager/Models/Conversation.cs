using System;
using System.Collections.Generic;

namespace Dating_Manager.Models;

public partial class Conversation
{
    public Guid Id { get; set; }

    public Guid User1Id { get; set; }

    public Guid User2Id { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? LastMessageAt { get; set; }

    public virtual ICollection<Message> Messages { get; set; } = new List<Message>();

    public virtual User User1 { get; set; } = null!;

    public virtual User User2 { get; set; } = null!;
}
