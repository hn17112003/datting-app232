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
public class ConversationsController : ControllerBase
{
    private readonly DatingManagerContext _db;

    public ConversationsController(DatingManagerContext db)
    {
        _db = db;
    }

    // GET /api/conversations - Lấy danh sách cuộc hội thoại của user hiện tại
    [HttpGet]
    [Authorize(Roles = "Customer")]
    public async Task<IActionResult> GetConversations()
    {
        var email = User.FindFirstValue(ClaimTypes.Name);
        if (string.IsNullOrEmpty(email)) return Unauthorized();

        var me = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Email == email);
        if (me == null) return Unauthorized();

        // Lấy danh sách user IDs đã bị chặn
        var blockedUserIds = await _db.Blocks
            .Where(b => b.BlockerId == me.Id || b.BlockedId == me.Id)
            .Select(b => b.BlockerId == me.Id ? b.BlockedId : b.BlockerId)
            .ToListAsync();

        var list = await _db.Conversations.AsNoTracking()
            .Where(c => c.User1Id == me.Id || c.User2Id == me.Id)
            .Select(c => new
            {
                id = c.Id,
                user1Id = c.User1Id,
                user2Id = c.User2Id,
                otherUserId = c.User1Id == me.Id ? c.User2Id : c.User1Id,
                createdAt = c.CreatedAt,
                lastMessageAt = c.LastMessageAt
            })
            .Where(c => !blockedUserIds.Contains(c.otherUserId)) // Loại bỏ conversations với người đã bị chặn
            .OrderByDescending(c => c.lastMessageAt ?? c.createdAt)
            .ToListAsync();

        return Ok(list);
    }

    // GET /api/conversations/{id} - Lấy chi tiết 1 cuộc hội thoại
    [HttpGet("{id:guid}")]
    [Authorize(Roles = "Customer")]
    public async Task<IActionResult> GetById([FromRoute] Guid id)
    {
        var email = User.FindFirstValue(ClaimTypes.Name);
        if (string.IsNullOrEmpty(email)) return Unauthorized();

        var me = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Email == email);
        if (me == null) return Unauthorized();

        var conv = await _db.Conversations.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id && (c.User1Id == me.Id || c.User2Id == me.Id));
        if (conv == null) return NotFound(new { message = "Không tìm thấy cuộc hội thoại" });

        return Ok(new
        {
            id = conv.Id,
            user1Id = conv.User1Id,
            user2Id = conv.User2Id,
            createdAt = conv.CreatedAt,
            lastMessageAt = conv.LastMessageAt
        });
    }

    // POST /api/conversations - Tạo cuộc hội thoại mới (nếu chưa có)
    [HttpPost]
    [Authorize(Roles = "Customer")]
    public async Task<IActionResult> Create([FromBody] CreateConversationRequest request)
    {
        var email = User.FindFirstValue(ClaimTypes.Name);
        if (string.IsNullOrEmpty(email)) return Unauthorized();

        var me = await _db.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (me == null) return Unauthorized();

        if (me.Id == request.User2Id) return BadRequest(new { message = "Không thể tạo hội thoại với chính mình" });

        var user2 = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.User2Id);
        if (user2 == null) return NotFound(new { message = "Không tìm thấy người dùng" });

        // Kiểm tra đã có conversation chưa
        var existing = await _db.Conversations
            .FirstOrDefaultAsync(c => (c.User1Id == me.Id && c.User2Id == request.User2Id) ||
                                      (c.User1Id == request.User2Id && c.User2Id == me.Id));
        if (existing != null)
        {
            return Ok(new { id = existing.Id, message = "Cuộc hội thoại đã tồn tại" });
        }

        // Kiểm tra có bị chặn không
        var isBlocked = await _db.Blocks.AnyAsync(b =>
            (b.BlockerId == me.Id && b.BlockedId == request.User2Id) ||
            (b.BlockerId == request.User2Id && b.BlockedId == me.Id));
        if (isBlocked) return Forbid();

        var conv = new Conversation
        {
            Id = Guid.NewGuid(),
            User1Id = me.Id,
            User2Id = request.User2Id,
            CreatedAt = DateTime.Now
        };

        _db.Conversations.Add(conv);
        await _db.SaveChangesAsync();

        return Ok(new { id = conv.Id, message = "Tạo cuộc hội thoại thành công" });
    }
}

