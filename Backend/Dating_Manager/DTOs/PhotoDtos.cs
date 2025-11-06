using Microsoft.AspNetCore.Http;

namespace Dating_Manager.DTOs;

public class PhotoDtos
{
    public IFormFile? File { get; set; }
    public string? PhotoType { get; set; }
}


