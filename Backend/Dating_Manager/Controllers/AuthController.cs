using System.Security.Claims;
using System.Security.Cryptography;
using Dating_Manager.DTOs;
using Dating_Manager.Models;
using Dating_Manager.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Dating_Manager.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly DatingManagerContext _db;
    private readonly JwtService _jwtService;
    private readonly IMemoryCache _cache;
    private readonly IEmailService _emailService;

    private const string VerifyPrefix = "verify:";
    private const string ResetPrefix = "reset:";

    public AuthController(DatingManagerContext db, JwtService jwtService, IMemoryCache cache, IEmailService emailService)
    {
        _db = db;
        _jwtService = jwtService;
        _cache = cache;
        _emailService = emailService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromForm] RegisterRequest request)
    {
        var exists = await _db.Users.AnyAsync(u => u.Email == request.Email);
        if (exists) return Conflict(new { message = "Email đã tồn tại" });

        var hasher = new PasswordHasher<User>();
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = request.Email,
            CreatedAt = DateTime.Now,
            LastActive = DateTime.Now,
            IsVerified = false,
            Status = "Active",
            LastLoginIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? string.Empty,
            PasswordSalt = Convert.ToBase64String(RandomNumberGenerator.GetBytes(16)),
            RoleId = 2 // Customer
        };
        user.PasswordHash = hasher.HashPassword(user, request.Password);

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        var code = RandomNumberGenerator.GetInt32(100000, 999999).ToString();
        _cache.Set(VerifyPrefix + user.Email, code, TimeSpan.FromMinutes(15));

        // Gửi email xác thực
        await _emailService.SendVerificationEmailAsync(user.Email, code);

        return Ok(new { message = "Đăng ký thành công. Vui lòng kiểm tra email để lấy mã xác thực.", email = user.Email });
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login([FromForm] LoginRequest request)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
        if (user == null)
            return Unauthorized(new { message = "Email hoặc mật khẩu không đúng" });

        if (user.Status?.ToLower() == "inactive")
        {
            return StatusCode(403, new { message = "Tài khoản của bạn đã bị vô hiệu hóa, vui lòng liên hệ quản trị viên." });
        }

        if (user.Status?.ToLower() == "Inactive")
        {
            return StatusCode(403, new { message = "Tài khoản của bạn đã bị khóa, vui lòng liên hệ quản trị viên." });
        }


        var hasher = new PasswordHasher<User>();
        var result = hasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (result == PasswordVerificationResult.Failed)
            return Unauthorized(new { message = "Email hoặc mật khẩu không đúng" });

        // ✅ Cập nhật thông tin hoạt động
        user.LastActive = DateTime.Now;
        user.LastLoginIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? string.Empty;
        await _db.SaveChangesAsync();

        // ✅ Lấy role name từ DB
        var roleName = "Customer"; // default
        if (user.RoleId.HasValue)
        {
            var role = await _db.Roles.AsNoTracking().FirstOrDefaultAsync(r => r.Id == user.RoleId.Value);
            roleName = role?.Name ?? "Customer";
        }

        // ✅ Sinh token với role từ DB
        var token = _jwtService.GenerateToken(user.Email, roleName);

        return Ok(new AuthResponse
        {
            Token = token,
            Email = user.Email
        });
    }

    [HttpPost("verify-email")]
    public async Task<IActionResult> VerifyEmail([FromForm] VerifyEmailRequest request)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
        if (user == null) return NotFound(new { message = "Không tìm thấy người dùng" });

        if (!_cache.TryGetValue(VerifyPrefix + request.Email, out string? code) || code != request.Code)
        {
            return BadRequest(new { message = "Mã xác thực không hợp lệ hoặc đã hết hạn" });
        }

        user.IsVerified = true;
        await _db.SaveChangesAsync();
        _cache.Remove(VerifyPrefix + request.Email);
        return Ok(new { message = "Xác thực email thành công" });
    }

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromForm] ForgotPasswordRequest request)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
        if (user == null)
        {
            // Tránh lộ thông tin người dùng
            return Ok(new { message = "Nếu email tồn tại, liên kết đã được gửi" });
        }

        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        _cache.Set(ResetPrefix + user.Email, token, TimeSpan.FromMinutes(30));

        // Gửi email đặt lại mật khẩu
        await _emailService.SendPasswordResetEmailAsync(user.Email, token);

        return Ok(new { message = "Nếu email tồn tại, liên kết đặt lại mật khẩu đã được gửi đến email của bạn." });
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromForm] ResetPasswordRequest request)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
        if (user == null) return NotFound(new { message = "Không tìm thấy người dùng" });

        if (!_cache.TryGetValue(ResetPrefix + request.Email, out string? token) || token != request.Token)
        {
            return BadRequest(new { message = "Token không hợp lệ hoặc đã hết hạn" });
        }

        var hasher = new PasswordHasher<User>();
        user.PasswordHash = hasher.HashPassword(user, request.NewPassword);
        user.PasswordSalt = Convert.ToBase64String(RandomNumberGenerator.GetBytes(16));
        await _db.SaveChangesAsync();
        _cache.Remove(ResetPrefix + request.Email);
        return Ok(new { message = "Đổi mật khẩu thành công" });
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> Me()
    {
        var username = User.FindFirstValue(ClaimTypes.Name);
        if (string.IsNullOrEmpty(username)) return Unauthorized();

        var user = await _db.Users.AsNoTracking()
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Email == username);

        if (user == null) return Unauthorized();

        return Ok(new
        {
            id = user.Id,
            email = user.Email,
            isVerified = user.IsVerified,
            createdAt = user.CreatedAt,
            lastActive = user.LastActive,
            roleId = user.RoleId,
            roleName = user.Role?.Name
        });
    }
}


