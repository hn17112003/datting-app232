using System.ComponentModel.DataAnnotations;

namespace Dating_Manager.DTOs;

public class SendMessageRequest
{
    [Required]
    public Guid ConversationId { get; set; }

    [Required]
    public Guid RecipientId { get; set; }

    [Required]
    public string Content { get; set; } = string.Empty; // nvarchar(max)
}

