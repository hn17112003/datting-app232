using System;
using System.Collections.Generic;

namespace Dating_Manager.Models;

public partial class Message
{
    public Guid Id { get; set; }

    public Guid? ConversationId { get; set; }

    public Guid SenderId { get; set; }

    public Guid RecipientId { get; set; }

    public string Content { get; set; } = null!;

    public DateTime SentAt { get; set; }

    public DateTime? ReadAt { get; set; }

    public bool IsDeleted { get; set; }

    public virtual Conversation? Conversation { get; set; }

    public virtual User Recipient { get; set; } = null!;

    public virtual User Sender { get; set; } = null!;
}
