using System;
using System.Collections.Generic;

namespace Dating_Manager.Models;

public partial class Profile
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public string KnownAs { get; set; } = null!;

    public DateOnly DateOfBirth { get; set; }

    public string? Gender { get; set; }

    public string? Bio { get; set; }

    public string? City { get; set; }

    public string? Country { get; set; }

    public string? LookingForGender { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<Photo> Photos { get; set; } = new List<Photo>();

    public virtual User User { get; set; } = null!;
}
