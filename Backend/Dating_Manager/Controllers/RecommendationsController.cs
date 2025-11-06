using System.Security.Claims;
using Dating_Manager.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Dating_Manager.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class RecommendationsController : ControllerBase
{
    private readonly DatingManagerContext _db;

    public RecommendationsController(DatingManagerContext db)
    {
        _db = db;
    }

    // GET /api/recommendations?take=20
    [HttpGet]
    [Authorize(Roles = "Customer")]
    public async Task<IActionResult> Get([FromQuery] int take = 20)
    {
        var email = User.FindFirstValue(ClaimTypes.Name);
        if (string.IsNullOrEmpty(email)) return Unauthorized();

        var me = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Email == email);
        if (me == null) return Unauthorized();

        var myProfile = await _db.Profiles.AsNoTracking().FirstOrDefaultAsync(p => p.UserId == me.Id);
        if (myProfile == null)
        {
            return BadRequest(new { message = "Bạn cần cập nhật hồ sơ trước khi sử dụng tính năng này" });
        }

        take = Math.Clamp(take, 1, 50);

        // Lấy danh sách user IDs đã bị chặn
        var blockedUserIds = await _db.Blocks
            .Where(b => b.BlockerId == me.Id || b.BlockedId == me.Id)
            .Select(b => b.BlockerId == me.Id ? b.BlockedId : b.BlockerId)
            .ToListAsync();

        // Lấy danh sách user IDs đã match
        var matchedUserIds = await _db.Matches
            .Where(m => (m.User1Id == me.Id || m.User2Id == me.Id) && m.Status == "Active")
            .Select(m => m.User1Id == me.Id ? m.User2Id : m.User1Id)
            .ToListAsync();

        var query = _db.Profiles.AsNoTracking().Include(p => p.User)
            .Where(p => p.UserId != me.Id)
            .Where(p => p.User.Status != "Hidden" && p.User.Status != "Inactive" && p.User.Status != "Deleted")
            .Where(p => !blockedUserIds.Contains(p.UserId)) // Loại bỏ người đã bị chặn
            .Where(p => !matchedUserIds.Contains(p.UserId)); // Loại bỏ người đã match

        if (myProfile != null)
        {
            if (!string.IsNullOrWhiteSpace(myProfile.LookingForGender))
            {
                query = query.Where(p => p.Gender == myProfile.LookingForGender);
            }
            else if (!string.IsNullOrWhiteSpace(myProfile.Gender))
            {
                query = query.Where(p => p.LookingForGender == myProfile.Gender);
            }
        }

        var list = await query
            .OrderByDescending(p => p.User.LastActive)
            .Take(take)
            .Select(p => new
            {
                userId = p.UserId,
                knownAs = p.KnownAs,
                gender = p.Gender,
                city = p.City,
                country = p.Country,
                lastActive = p.User.LastActive
            })
            .ToListAsync();

        return Ok(list);
    }
}


