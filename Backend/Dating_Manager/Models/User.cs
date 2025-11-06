using System;
using System.Collections.Generic;

namespace Dating_Manager.Models;

public partial class User
{
    public Guid Id { get; set; }

    public string Email { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public string PasswordSalt { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime LastActive { get; set; }

    public bool IsVerified { get; set; }

    public string? Status { get; set; }

    public string? LastLoginIp { get; set; }

    public int? RoleId { get; set; }

    public virtual ICollection<Block> BlockBlockeds { get; set; } = new List<Block>();

    public virtual ICollection<Block> BlockBlockers { get; set; } = new List<Block>();

    public virtual ICollection<Conversation> ConversationUser1s { get; set; } = new List<Conversation>();

    public virtual ICollection<Conversation> ConversationUser2s { get; set; } = new List<Conversation>();

    public virtual ICollection<Like> LikeSourceUsers { get; set; } = new List<Like>();

    public virtual ICollection<Like> LikeTargetUsers { get; set; } = new List<Like>();

    public virtual ICollection<Match> MatchUser1s { get; set; } = new List<Match>();

    public virtual ICollection<Match> MatchUser2s { get; set; } = new List<Match>();

    public virtual ICollection<Message> MessageRecipients { get; set; } = new List<Message>();

    public virtual ICollection<Message> MessageSenders { get; set; } = new List<Message>();

    public virtual Profile? Profile { get; set; }

    public virtual Role? Role { get; set; }

    public virtual ICollection<Interest> Interests { get; set; } = new List<Interest>();
}
