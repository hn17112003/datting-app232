using Dating_Manager.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Dating_Manager.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class AdminController : ControllerBase
{
    private readonly DatingManagerContext _db;

    public AdminController(DatingManagerContext db)
    {
        _db = db;
    }

    // GET /api/admin/users
    [HttpGet("users")]
    public async Task<IActionResult> GetAllUsers([FromQuery] int skip = 0, [FromQuery] int take = 50)
    {
        take = Math.Clamp(take, 1, 200);
        var list = await _db.Users.AsNoTracking()
            .OrderByDescending(u => u.CreatedAt)
            .Skip(skip).Take(take)
            .Select(u => new
            {
                id = u.Id,
                email = u.Email,
                status = u.Status,
                isVerified = u.IsVerified,
                createdAt = u.CreatedAt,
                lastActive = u.LastActive
            })
            .ToListAsync();
        return Ok(list);
    }


    // POST /api/admin/ban-user/{id}
    [HttpPost("ban-user/{id:guid}")]
    public async Task<IActionResult> BanUser([FromRoute] Guid id)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id);
        if (user == null) return NotFound(new { message = "Không tìm thấy người dùng" });

        user.Status = "Inactive";
        await _db.SaveChangesAsync();
        return Ok(new { message = "Đã khóa tài khoản", id = user.Id, status = user.Status });
    }

    // POST /api/admin/unban-user/{id}
    [HttpPost("unban-user/{id:guid}")]
    public async Task<IActionResult> UnbanUser([FromRoute] Guid id)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id);
        if (user == null) return NotFound(new { message = "Không tìm thấy người dùng" });

        if (user.Status != "Inactive")
        {
            return BadRequest(new { message = "Tài khoản này không ở trạng thái bị khóa" });
        }

        user.Status = "Active";
        user.LastActive = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return Ok(new { message = "Đã mở khóa tài khoản", id = user.Id, status = user.Status });
    }

    // GET /api/admin/statistics
    [HttpGet("statistics")]
    public async Task<IActionResult> Statistics()
    {
        var users = await _db.Users.CountAsync();
        var matches = await _db.Matches.CountAsync();
        var messages = await _db.Messages.CountAsync();
        return Ok(new { totalUsers = users, totalMatches = matches, totalMessages = messages });
    }
}


