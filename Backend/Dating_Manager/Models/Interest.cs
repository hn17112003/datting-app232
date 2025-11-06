using System;
using System.Collections.Generic;

namespace Dating_Manager.Models;

public partial class Interest
{
    public int Id { get; set; }

    public string? Name { get; set; }

    public virtual ICollection<User> Users { get; set; } = new List<User>();
}
