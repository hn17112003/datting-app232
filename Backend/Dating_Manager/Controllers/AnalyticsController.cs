using Dating_Manager.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Dating_Manager.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class AnalyticsController : ControllerBase
{
    private readonly DatingManagerContext _db;

    public AnalyticsController(DatingManagerContext db)
    {
        _db = db;
    }

    // GET /api/analytics/match-rate
    [HttpGet("match-rate")]
    public async Task<IActionResult> MatchRate()
    {
        var totalLikes = await _db.Likes.CountAsync();
        var totalMatches = await _db.Matches.CountAsync();
        var rate = totalLikes == 0 ? 0.0 : (double)totalMatches / totalLikes;
        return Ok(new { totalLikes, totalMatches, matchRate = rate });
    }

    // GET /api/analytics/active-users?days=7&take=50
    [HttpGet("active-users")]
    public async Task<IActionResult> ActiveUsers([FromQuery] int days = 7, [FromQuery] int take = 50)
    {
        days = Math.Clamp(days, 1, 90);
        take = Math.Clamp(take, 1, 200);
        var threshold = DateTime.Now.AddDays(-days);
        var users = await _db.Users.AsNoTracking()
            .Where(u => u.LastActive >= threshold)
            .OrderByDescending(u => u.LastActive)
            .Take(take)
            .Select(u => new { id = u.Id, email = u.Email, lastActive = u.LastActive })
            .ToListAsync();
        return Ok(users);
    }
}


