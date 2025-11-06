using System;
using System.Collections.Generic;

namespace Dating_Manager.Models;

public partial class Photo
{
    public Guid Id { get; set; }

    public Guid ProfileId { get; set; }

    public string Url { get; set; } = null!;

    public string? PublicId { get; set; }

    public string? PhotoType { get; set; }

    public bool IsMain { get; set; }

    public DateTime UploadedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual Profile Profile { get; set; } = null!;
}
