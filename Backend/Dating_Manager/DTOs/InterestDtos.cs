using System.ComponentModel.DataAnnotations;

namespace Dating_Manager.DTOs;

public class CreateInterestRequest
{
    [Required]
    public string Name { get; set; } = string.Empty; // nvarchar(100)
}

public class UpdateUserInterestsRequest
{
    [Required]
    public List<int> InterestIds { get; set; } = new();
}


