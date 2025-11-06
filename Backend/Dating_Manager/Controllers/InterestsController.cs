using Dating_Manager.DTOs;
using Dating_Manager.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Dating_Manager.Controllers;

[ApiController]
[Route("api/[controller]")]
public class InterestsController : ControllerBase
{
    private readonly DatingManagerContext _db;

    public InterestsController(DatingManagerContext db)
    {
        _db = db;
    }

    // GET /api/interests
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var items = await _db.Interests.AsNoTracking()
            .OrderBy(i => i.Name)
            .Select(i => new { id = i.Id, name = i.Name })
            .ToListAsync();
        return Ok(items);
    }

    // POST /api/interests (admin)
    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateInterestRequest request)
    {
        var exists = await _db.Interests.AnyAsync(i => i.Name == request.Name);
        if (exists) return Conflict(new { message = "Sở thích đã tồn tại" });

        var interest = new Interest { Name = request.Name };
        _db.Interests.Add(interest);
        await _db.SaveChangesAsync();
        return Ok(new { id = interest.Id, name = interest.Name });
    }
}


