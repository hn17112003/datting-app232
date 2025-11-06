using System.Security.Claims;
using Dating_Manager.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Dating_Manager.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MatchesController : ControllerBase
{
    private readonly DatingManagerContext _db;

    public MatchesController(DatingManagerContext db)
    {
        _db = db;
    }

    // GET /api/matches - danh sách người đã ghép đôi
    [HttpGet]
    [Authorize(Roles = "Customer")]
    public async Task<IActionResult> GetMyMatches()
    {
        var email = User.FindFirstValue(ClaimTypes.Name);
        if (string.IsNullOrEmpty(email)) return Unauthorized();
        var me = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Email == email);
        if (me == null) return Unauthorized();

        var list = await _db.Matches.AsNoTracking()
            .Where(m => (m.User1Id == me.Id || m.User2Id == me.Id) && m.Status == "Active")
            .Select(m => new
            {
                matchId = m.Id,
                userId = m.User1Id == me.Id ? m.User2Id : m.User1Id
            })
            .Join(_db.Users, x => x.userId, u => u.Id, (x, u) => new
            {
                matchId = x.matchId,
                userId = u.Id,
                email = u.Email,
                lastActive = u.LastActive
            })
            .OrderByDescending(x => x.lastActive)
            .ToListAsync();

        return Ok(list);
    }

    // DELETE /api/matches/{matchId}
    [HttpDelete("{matchId:guid}")]
    [Authorize(Roles = "Customer")]
    public async Task<IActionResult> DeleteMatch([FromRoute] Guid matchId)
    {
        var email = User.FindFirstValue(ClaimTypes.Name);
        if (string.IsNullOrEmpty(email)) return Unauthorized();
        var me = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Email == email);
        if (me == null) return Unauthorized();

        var match = await _db.Matches.FirstOrDefaultAsync(m => m.Id == matchId);
        if (match == null) return NotFound(new { message = "Không tìm thấy match" });
        if (match.User1Id != me.Id && match.User2Id != me.Id) return Forbid();

        _db.Matches.Remove(match);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}


