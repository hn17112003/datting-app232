using System.Security.Claims;
using Dating_Manager.DTOs;
using Dating_Manager.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Dating_Manager.Controllers;

[ApiController]
[Route("api/users/interests")]
[Authorize]
public class UserInterestsController : ControllerBase
{
    private readonly DatingManagerContext _db;

    public UserInterestsController(DatingManagerContext db)
    {
        _db = db;
    }

    // POST /api/users/interests - cập nhật danh sách sở thích của user hiện tại (replace set)
    [HttpPost]
    public async Task<IActionResult> UpdateMyInterests([FromBody] UpdateUserInterestsRequest request)
    {
        var email = User.FindFirstValue(ClaimTypes.Name);
        if (string.IsNullOrEmpty(email)) return Unauthorized();

        var user = await _db.Users.Include(u => u.Interests).FirstOrDefaultAsync(u => u.Email == email);
        if (user == null) return Unauthorized();

        var validInterests = await _db.Interests.Where(i => request.InterestIds.Contains(i.Id)).ToListAsync();
        if (validInterests.Count != request.InterestIds.Count)
        {
            return BadRequest(new { message = "Một hoặc nhiều InterestId không tồn tại" });
        }

        user.Interests.Clear();
        foreach (var i in validInterests)
        {
            user.Interests.Add(i);
        }

        await _db.SaveChangesAsync();
        return Ok(new { message = "Cập nhật sở thích thành công" });
    }

    // GET /api/users/interests/{userId}
    [AllowAnonymous]
    [HttpGet("{userId:guid}")]
    public async Task<IActionResult> GetUserInterests([FromRoute] Guid userId)
    {
        var user = await _db.Users.AsNoTracking()
            .Include(u => u.Interests)
            .FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null) return NotFound(new { message = "Không tìm thấy người dùng" });

        var list = user.Interests
            .OrderBy(i => i.Name)
            .Select(i => new { id = i.Id, name = i.Name })
            .ToList();

        return Ok(list);
    }
}


