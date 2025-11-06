using System.Security.Claims;
using Dating_Manager.DTOs;
using Dating_Manager.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Dating_Manager.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly DatingManagerContext _db;

    public UsersController(DatingManagerContext db)
    {
        _db = db;
    }

    // GET /api/users?skip=0&take=50
    [HttpGet]
    public async Task<IActionResult> GetUsers([FromQuery] int skip = 0, [FromQuery] int take = 50)
    {
        take = Math.Clamp(take, 1, 100);

        var users = await _db.Users
            .AsNoTracking()
            .OrderByDescending(u => u.LastActive)
            .Skip(skip)
            .Take(take)
            .Select(u => new
            {
                id = u.Id,
                email = u.Email,
                isVerified = u.IsVerified,
                createdAt = u.CreatedAt,
                lastActive = u.LastActive,
                status = u.Status
            })
            .ToListAsync();

        return Ok(users);
    }

    // GET /api/users/{id}
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById([FromRoute] Guid id)
    {
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id);
        if (user == null) return NotFound(new { message = "Không tìm thấy người dùng" });

        return Ok(new
        {
            id = user.Id,
            email = user.Email,
            isVerified = user.IsVerified,
            createdAt = user.CreatedAt,
            lastActive = user.LastActive,
            status = user.Status
        });
    }

    // DELETE /api/users/{id}
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete([FromRoute] Guid id)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id);
        if (user == null) return NotFound(new { message = "Không tìm thấy người dùng" });

        _db.Users.Remove(user);
        try
        {
            await _db.SaveChangesAsync();
            return NoContent();
        }
        catch (DbUpdateException)
        {
            return BadRequest(new { message = "Không thể xoá người dùng do ràng buộc dữ liệu" });
        }
    }

    // PATCH /api/users/status  (áp dụng cho user hiện tại)
    [HttpPatch("status")]
    public async Task<IActionResult> UpdateStatus([FromForm] UpdateStatusRequest request)
    {
        var email = User.FindFirstValue(ClaimTypes.Name);
        if (string.IsNullOrEmpty(email)) return Unauthorized();

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (user == null) return Unauthorized();

        user.Status = request.Status;
        if (request.Status == "Active")
        {
            user.LastActive = DateTime.Now;
        }

        await _db.SaveChangesAsync();
        return Ok(new { message = "Cập nhật trạng thái thành công", status = user.Status });
    }
}


