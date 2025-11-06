using System.Security.Claims;
using Dating_Manager.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Dating_Manager.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class LikesController : ControllerBase
{
    private readonly DatingManagerContext _db;

    public LikesController(DatingManagerContext db)
    {
        _db = db;
    }

    // POST /api/likes/{targetId}
    [HttpPost("{targetId:guid}")]
    [Authorize(Roles = "Customer")]
    public async Task<IActionResult> LikeUser([FromRoute] Guid targetId)
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

        if (me.Id == targetId) return BadRequest(new { message = "Không thể tự thích chính mình" });

        var target = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == targetId);
        if (target == null) return NotFound(new { message = "Không tìm thấy người dùng" });

        var existed = await _db.Likes.AnyAsync(l => l.SourceUserId == me.Id && l.TargetUserId == targetId);
        if (existed) return Ok(new { message = "Đã thích trước đó" });

        _db.Likes.Add(new Like { SourceUserId = me.Id, TargetUserId = targetId, CreatedAt = DateTime.Now });

        // Tạo match nếu đối phương đã thích mình trước đó
        var reciprocal = await _db.Likes.AnyAsync(l => l.SourceUserId == targetId && l.TargetUserId == me.Id);
        if (reciprocal)
        {
            _db.Matches.Add(new Match
            {
                Id = Guid.NewGuid(),
                User1Id = me.Id,
                User2Id = targetId,
                Status = "Active",
                CreatedAt = DateTime.Now
            });
        }

        await _db.SaveChangesAsync();
        return Ok(new { message = reciprocal ? "Đã ghép đôi thành công" : "Đã thích người dùng" });
    }

    // GET /api/likes - danh sách người mình đã thích
    [HttpGet]
    [Authorize(Roles = "Customer")]
    public async Task<IActionResult> GetMyLikes()
    {
        var email = User.FindFirstValue(ClaimTypes.Name);
        if (string.IsNullOrEmpty(email)) return Unauthorized();
        var me = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Email == email);
        if (me == null) return Unauthorized();

        var list = await _db.Likes.AsNoTracking()
            .Where(l => l.SourceUserId == me.Id)
            .Join(_db.Users, l => l.TargetUserId, u => u.Id, (l, u) => new { u.Id, u.Email, u.LastActive })
            .OrderByDescending(x => x.LastActive)
            .ToListAsync();

        return Ok(list);
    }

    // GET /api/likes/liked-by - danh sách người đã thích mình
    [HttpGet("liked-by")]
    [Authorize(Roles = "Customer")]
    public async Task<IActionResult> GetLikedBy()
    {
        var email = User.FindFirstValue(ClaimTypes.Name);
        if (string.IsNullOrEmpty(email)) return Unauthorized();
        var me = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Email == email);
        if (me == null) return Unauthorized();

        var list = await _db.Likes.AsNoTracking()
            .Where(l => l.TargetUserId == me.Id)
            .Join(_db.Users, l => l.SourceUserId, u => u.Id, (l, u) => new { u.Id, u.Email, u.LastActive })
            .OrderByDescending(x => x.LastActive)
            .ToListAsync();

        return Ok(list);
    }
}


