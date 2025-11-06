using System.ComponentModel.DataAnnotations;

namespace Dating_Manager.DTOs;

public class UpdateProfileRequest
{
    [Required]
    public string KnownAs { get; set; } = string.Empty; // nvarchar(100)

    [Required]
    public DateOnly DateOfBirth { get; set; } // date

    public string? Gender { get; set; } // nvarchar(50)

    public string? Bio { get; set; } // nvarchar(1500)

    public string? City { get; set; } // nvarchar(100)

    public string? Country { get; set; } // nvarchar(100)

    public string? LookingForGender { get; set; } // nvarchar(50)
}


