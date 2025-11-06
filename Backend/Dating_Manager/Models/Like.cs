using System;
using System.Collections.Generic;

namespace Dating_Manager.Models;

public partial class Like
{
    public Guid SourceUserId { get; set; }

    public Guid TargetUserId { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual User SourceUser { get; set; } = null!;

    public virtual User TargetUser { get; set; } = null!;
}
