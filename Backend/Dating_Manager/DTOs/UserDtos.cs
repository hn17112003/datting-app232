using System.ComponentModel.DataAnnotations;

namespace Dating_Manager.DTOs;

public class UpdateStatusRequest
{
    [Required]
    public string Status { get; set; } = string.Empty; // Active, Hidden, Inactive, Deleted
}



