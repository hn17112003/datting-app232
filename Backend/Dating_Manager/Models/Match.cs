using System;
using System.Collections.Generic;

namespace Dating_Manager.Models;

public partial class Match
{
    public Guid Id { get; set; }

    public Guid User1Id { get; set; }

    public Guid User2Id { get; set; }

    public string? Status { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual User User1 { get; set; } = null!;

    public virtual User User2 { get; set; } = null!;
}
