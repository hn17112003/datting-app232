using System;
using System.Collections.Generic;

namespace Dating_Manager.Models;

public partial class Block
{
    public Guid BlockerId { get; set; }

    public Guid BlockedId { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual User Blocked { get; set; } = null!;

    public virtual User Blocker { get; set; } = null!;
}
