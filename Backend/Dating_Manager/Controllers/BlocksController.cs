using System.Security.Claims;
using Dating_Manager.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Dating_Manager.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class BlocksController : ControllerBase
{
	private readonly DatingManagerContext _db;

	public BlocksController(DatingManagerContext db)
	{
		_db = db;
	}

	// POST /api/blocks/{targetId} - Chặn người dùng
	[HttpPost("{targetId:guid}")]
	[Authorize(Roles = "Customer")]
	public async Task<IActionResult> BlockUser([FromRoute] Guid targetId)
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

		if (me.Id == targetId) return BadRequest(new { message = "Không thể tự chặn chính mình" });

		var target = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == targetId);
		if (target == null) return NotFound(new { message = "Không tìm thấy người dùng" });

		// Kiểm tra xem đã match chưa
		var hasMatch = await _db.Matches.AnyAsync(m => 
			((m.User1Id == me.Id && m.User2Id == targetId) || (m.User1Id == targetId && m.User2Id == me.Id)) 
			&& m.Status == "Active");
		
		if (!hasMatch)
		{
			return BadRequest(new { message = "Chỉ có thể chặn người dùng đã match với bạn" });
		}

		var existed = await _db.Blocks.AnyAsync(b => b.BlockerId == me.Id && b.BlockedId == targetId);
		if (existed) return Ok(new { message = "Đã chặn trước đó" });

		_db.Blocks.Add(new Block
		{
			BlockerId = me.Id,
			BlockedId = targetId,
			CreatedAt = DateTime.Now
		});

		// Ẩn chat: đánh dấu IsDeleted tất cả tin nhắn giữa hai bên
		var messages = await _db.Messages
			.Where(m => (m.SenderId == me.Id && m.RecipientId == targetId) || (m.SenderId == targetId && m.RecipientId == me.Id))
			.ToListAsync();
		foreach (var m in messages)
		{
			m.IsDeleted = true;
		}

		// Xoá match giữa hai người nếu có
		var matches = await _db.Matches
			.Where(m => (m.User1Id == me.Id && m.User2Id == targetId) || (m.User1Id == targetId && m.User2Id == me.Id))
			.ToListAsync();
		if (matches.Count > 0)
		{
			_db.Matches.RemoveRange(matches);
		}

		// Xoá like hai chiều (tuỳ chọn: tránh tái tạo match)
		var likes = await _db.Likes
			.Where(l => (l.SourceUserId == me.Id && l.TargetUserId == targetId) || (l.SourceUserId == targetId && l.TargetUserId == me.Id))
			.ToListAsync();
		if (likes.Count > 0)
		{
			_db.Likes.RemoveRange(likes);
		}

		await _db.SaveChangesAsync();
		return Ok(new { message = "Đã chặn người dùng và ẩn chat/match liên quan" });
	}

	// GET /api/blocks - Danh sách người đã bị chặn
	[HttpGet]
	[Authorize(Roles = "Customer")]
	public async Task<IActionResult> GetBlocked()
	{
		var email = User.FindFirstValue(ClaimTypes.Name);
		if (string.IsNullOrEmpty(email)) return Unauthorized();
		var me = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Email == email);
		if (me == null) return Unauthorized();

		var list = await _db.Blocks.AsNoTracking()
			.Where(b => b.BlockerId == me.Id)
			.Join(_db.Users, b => b.BlockedId, u => u.Id, (b, u) => new { u.Id, u.Email, u.LastActive, b.CreatedAt })
			.OrderByDescending(x => x.CreatedAt)
			.ToListAsync();

		return Ok(list);
	}

	// DELETE /api/blocks/{targetId} - Bỏ chặn
	[HttpDelete("{targetId:guid}")]
	[Authorize(Roles = "Customer")]
	public async Task<IActionResult> Unblock([FromRoute] Guid targetId)
	{
		var email = User.FindFirstValue(ClaimTypes.Name);
		if (string.IsNullOrEmpty(email)) return Unauthorized();
		var me = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Email == email);
		if (me == null) return Unauthorized();

		var block = await _db.Blocks.FirstOrDefaultAsync(b => b.BlockerId == me.Id && b.BlockedId == targetId);
		if (block == null) return NotFound(new { message = "Không có trạng thái chặn với người dùng này" });

		_db.Blocks.Remove(block);
		await _db.SaveChangesAsync();
		return NoContent();
	}
}


