using System.ComponentModel.DataAnnotations;

namespace Dating_Manager.DTOs;

public class CreateConversationRequest
{
    [Required]
    public Guid User2Id { get; set; }
}

