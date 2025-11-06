using System.Security.Claims;
using Dating_Manager.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace Dating_Manager.Hubs;

[Authorize]
public class ChatHub : Hub
{
    private readonly DatingManagerContext _db;
    private static readonly Dictionary<Guid, string> _userConnections = new();
    private static readonly object _lockObject = new();

    public ChatHub(DatingManagerContext db)
    {
        _db = db;
    }

    public override async Task OnConnectedAsync()
    {
        var email = Context.User?.FindFirstValue(ClaimTypes.Name);
        if (string.IsNullOrEmpty(email))
        {
            Context.Abort();
            return;
        }

        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Email == email);
        if (user == null)
        {
            Context.Abort();
            return;
        }

        // Lưu connection ID với user ID (thread-safe)
        lock (_lockObject)
        {
            _userConnections[user.Id] = Context.ConnectionId;
        }

        // Thêm user vào group của các conversation mà họ tham gia
        var conversations = await _db.Conversations
            .AsNoTracking()
            .Where(c => c.User1Id == user.Id || c.User2Id == user.Id)
            .Select(c => c.Id.ToString())
            .ToListAsync();

        foreach (var convId in conversations)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, convId);
        }

        // Thông báo user online
        await Clients.Others.SendAsync("UserOnline", user.Id);

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var email = Context.User?.FindFirstValue(ClaimTypes.Name);
        if (!string.IsNullOrEmpty(email))
        {
            var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Email == email);
            if (user != null)
            {
                lock (_lockObject)
                {
                    _userConnections.Remove(user.Id);
                }
                // Thông báo user offline
                await Clients.Others.SendAsync("UserOffline", user.Id);
            }
        }

        await base.OnDisconnectedAsync(exception);
    }

    public async Task SendMessage(Guid conversationId, Guid recipientId, string content)
    {
        var email = Context.User?.FindFirstValue(ClaimTypes.Name);
        if (string.IsNullOrEmpty(email)) return;

        var sender = await _db.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (sender == null) return;

        // Kiểm tra conversation có tồn tại và user có quyền truy cập
        var conv = await _db.Conversations
            .FirstOrDefaultAsync(c => c.Id == conversationId &&
                                      (c.User1Id == sender.Id || c.User2Id == sender.Id) &&
                                      (c.User1Id == recipientId || c.User2Id == recipientId));
        if (conv == null) return;

        // Kiểm tra có bị chặn không
        var isBlocked = await _db.Blocks.AnyAsync(b =>
            (b.BlockerId == sender.Id && b.BlockedId == recipientId) ||
            (b.BlockerId == recipientId && b.BlockedId == sender.Id));
        if (isBlocked) return;

        // Tạo message trong database
        var message = new Message
        {
            Id = Guid.NewGuid(),
            ConversationId = conversationId,
            SenderId = sender.Id,
            RecipientId = recipientId,
            Content = content,
            SentAt = DateTime.Now,
            IsDeleted = false
        };

        _db.Messages.Add(message);
        conv.LastMessageAt = DateTime.Now;
        await _db.SaveChangesAsync();

        // Gửi message đến group conversation
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

        await Clients.Group(conversationId.ToString()).SendAsync("ReceiveMessage", messageDto);

        // Gửi notification đến recipient nếu họ không online trong conversation này
        string? recipientConnectionId;
        lock (_lockObject)
        {
            _userConnections.TryGetValue(recipientId, out recipientConnectionId);
        }
        if (recipientConnectionId != null)
        {
            await Clients.Client(recipientConnectionId).SendAsync("NewMessageNotification", new
            {
                conversationId,
                senderId = sender.Id,
                content = content.Length > 50 ? content.Substring(0, 50) + "..." : content
            });
        }
    }

    public async Task MarkAsRead(Guid messageId)
    {
        var email = Context.User?.FindFirstValue(ClaimTypes.Name);
        if (string.IsNullOrEmpty(email)) return;

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (user == null) return;

        var message = await _db.Messages.FirstOrDefaultAsync(m => m.Id == messageId);
        if (message == null || message.RecipientId != user.Id) return;

        if (message.ReadAt == null)
        {
            message.ReadAt = DateTime.Now;
            await _db.SaveChangesAsync();

            // Thông báo sender rằng message đã được đọc
            if (message.ConversationId.HasValue)
            {
                string? senderConnectionId;
                lock (_lockObject)
                {
                    _userConnections.TryGetValue(message.SenderId, out senderConnectionId);
                }
                if (senderConnectionId != null)
                {
                    await Clients.Client(senderConnectionId).SendAsync("MessageRead", new
                    {
                        messageId = message.Id,
                        readAt = message.ReadAt
                    });
                }
            }
        }
    }

    public async Task Typing(Guid conversationId, bool isTyping)
    {
        var email = Context.User?.FindFirstValue(ClaimTypes.Name);
        if (string.IsNullOrEmpty(email)) return;

        var sender = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Email == email);
        if (sender == null) return;

        // Gửi signal typing đến các user khác trong conversation
        await Clients.OthersInGroup(conversationId.ToString()).SendAsync("UserTyping", new
        {
            conversationId,
            userId = sender.Id,
            isTyping
        });
    }

    public static bool IsUserOnline(Guid userId)
    {
        lock (_lockObject)
        {
            return _userConnections.ContainsKey(userId);
        }
    }

    public static string? GetConnectionId(Guid userId)
    {
        lock (_lockObject)
        {
            return _userConnections.TryGetValue(userId, out var connectionId) ? connectionId : null;
        }
    }
}

