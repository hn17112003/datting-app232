using System.Security.Claims;
using Dating_Manager.DTOs;
using Dating_Manager.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Dating_Manager.Hubs;

namespace Dating_Manager.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MessagesController : ControllerBase
{
    private readonly DatingManagerContext _db;
    private readonly IHubContext<ChatHub> _hubContext;

    public MessagesController(DatingManagerContext db, IHubContext<ChatHub> hubContext)
    {
        _db = db;
        _hubContext = hubContext;
    }

    // GET /api/messages/{conversationId} - Lấy tin nhắn trong cuộc hội thoại
    [HttpGet("{conversationId:guid}")]
    [Authorize(Roles = "Customer")]
    public async Task<IActionResult> GetByConversation([FromRoute] Guid conversationId, [FromQuery] int skip = 0, [FromQuery] int take = 50)
    {
        var email = User.FindFirstValue(ClaimTypes.Name);
        if (string.IsNullOrEmpty(email)) return Unauthorized();

        var me = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Email == email);
        if (me == null) return Unauthorized();

        var conv = await _db.Conversations.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == conversationId && (c.User1Id == me.Id || c.User2Id == me.Id));
        if (conv == null) return NotFound(new { message = "Không tìm thấy cuộc hội thoại" });

        take = Math.Clamp(take, 1, 100);

        var messages = await _db.Messages.AsNoTracking()
            .Where(m => m.ConversationId == conversationId && !m.IsDeleted)
            .OrderByDescending(m => m.SentAt)
            .Skip(skip)
            .Take(take)
            .Select(m => new
            {
                id = m.Id,
                senderId = m.SenderId,
                recipientId = m.RecipientId,
                content = m.Content,
                sentAt = m.SentAt,
                readAt = m.ReadAt,
                isDeleted = m.IsDeleted
            })
            .ToListAsync();

        return Ok(messages.OrderBy(m => m.sentAt));
    }

    // POST /api/messages - Gửi tin nhắn
    [HttpPost]
    [Authorize(Roles = "Customer")]
    public async Task<IActionResult> Send([FromBody] SendMessageRequest request)
    {
        var email = User.FindFirstValue(ClaimTypes.Name);
        if (string.IsNullOrEmpty(email)) return Unauthorized();

        var me = await _db.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (me == null) return Unauthorized();

        if (me.Id == request.RecipientId) return BadRequest(new { message = "Không thể gửi tin nhắn cho chính mình" });

        // Kiểm tra conversation có tồn tại và user có quyền truy cập
        var conv = await _db.Conversations
            .FirstOrDefaultAsync(c => c.Id == request.ConversationId &&
                                      (c.User1Id == me.Id || c.User2Id == me.Id) &&
                                      (c.User1Id == request.RecipientId || c.User2Id == request.RecipientId));
        if (conv == null) return NotFound(new { message = "Không tìm thấy cuộc hội thoại hoặc không có quyền truy cập" });

        // Kiểm tra có bị chặn không
        var isBlocked = await _db.Blocks.AnyAsync(b =>
            (b.BlockerId == me.Id && b.BlockedId == request.RecipientId) ||
            (b.BlockerId == request.RecipientId && b.BlockedId == me.Id));
        if (isBlocked) return Forbid();

        var message = new Message
        {
            Id = Guid.NewGuid(),
            ConversationId = request.ConversationId,
            SenderId = me.Id,
            RecipientId = request.RecipientId,
            Content = request.Content,
            SentAt = DateTime.Now,
            IsDeleted = false
        };

        _db.Messages.Add(message);

        // Cập nhật LastMessageAt của conversation
        conv.LastMessageAt = DateTime.Now;

        await _db.SaveChangesAsync();

        // Gửi message real-time qua SignalR
        var messageDto = new
        {
            id = message.Id,
            conversationId = message.ConversationId,
            senderId = message.SenderId,
            recipientId = message.RecipientId,
            content = message.Content,
            sentAt = message.SentAt,
            readAt = (DateTime?)null,
            isDeleted = false
        };

        await _hubContext.Clients.Group(conv.Id.ToString()).SendAsync("ReceiveMessage", messageDto);

        return Ok(new
        {
            id = message.Id,
            content = message.Content,
            sentAt = message.SentAt
        });
    }

    // PUT /api/messages/{id}/read - Đánh dấu tin nhắn đã đọc
    [HttpPut("{id:guid}/read")]
    [Authorize(Roles = "Customer")]
    public async Task<IActionResult> MarkAsRead([FromRoute] Guid id)
    {
        var email = User.FindFirstValue(ClaimTypes.Name);
        if (string.IsNullOrEmpty(email)) return Unauthorized();

        var me = await _db.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (me == null) return Unauthorized();

        var message = await _db.Messages.FirstOrDefaultAsync(m => m.Id == id);
        if (message == null) return NotFound(new { message = "Không tìm thấy tin nhắn" });

        if (message.RecipientId != me.Id) return Forbid();

        if (message.ReadAt == null)
        {
            message.ReadAt = DateTime.Now;
            await _db.SaveChangesAsync();

            // Thông báo sender rằng message đã được đọc qua SignalR
            if (message.ConversationId.HasValue)
            {
                await _hubContext.Clients.Group(message.ConversationId.Value.ToString()).SendAsync("MessageRead", new
                {
                    messageId = message.Id,
                    readAt = message.ReadAt
                });
            }
        }

        return Ok(new { message = "Đã đánh dấu đọc" });
    }

    // DELETE /api/messages/{id} - Xóa tin nhắn (soft delete)
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Customer")]
    public async Task<IActionResult> Delete([FromRoute] Guid id)
    {
        var email = User.FindFirstValue(ClaimTypes.Name);
        if (string.IsNullOrEmpty(email)) return Unauthorized();

        var me = await _db.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (me == null) return Unauthorized();

        var message = await _db.Messages.FirstOrDefaultAsync(m => m.Id == id);
        if (message == null) return NotFound(new { message = "Không tìm thấy tin nhắn" });

        // Chỉ người gửi mới có thể xóa
        if (message.SenderId != me.Id) return Forbid();

        message.IsDeleted = true;
        await _db.SaveChangesAsync();

        return NoContent();
    }
}

