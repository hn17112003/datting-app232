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
public class ProfilesController : ControllerBase
{
    private readonly DatingManagerContext _db;

    public ProfilesController(DatingManagerContext db)
    {
        _db = db;
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetMyProfile()
    {
        var email = User.FindFirstValue(ClaimTypes.Name);
        if (string.IsNullOrEmpty(email)) return Unauthorized();

        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Email == email);
        if (user == null) return Unauthorized();

        var profile = await _db.Profiles.AsNoTracking().FirstOrDefaultAsync(p => p.UserId == user.Id);
        if (profile == null)
        {
            return NotFound(new { message = "Chưa có hồ sơ" });
        }

        return Ok(new
        {
            id = profile.Id,
            userId = profile.UserId,
            knownAs = profile.KnownAs,
            dateOfBirth = profile.DateOfBirth,
            gender = profile.Gender,
            bio = profile.Bio,
            city = profile.City,
            country = profile.Country,
            lookingForGender = profile.LookingForGender,
            updatedAt = profile.UpdatedAt
        });
    }

    [HttpPut("me")]
    [Authorize(Roles = "Customer")]
    public async Task<IActionResult> UpdateMyProfile([FromForm] UpdateProfileRequest request)
    {
        var email = User.FindFirstValue(ClaimTypes.Name);
        if (string.IsNullOrEmpty(email)) return Unauthorized();

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (user == null) return Unauthorized();

        var profile = await _db.Profiles.FirstOrDefaultAsync(p => p.UserId == user.Id);
        if (profile == null)
        {
            // Tạo mới nếu chưa có
            profile = new Profile
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                KnownAs = request.KnownAs,
                DateOfBirth = request.DateOfBirth,
                Gender = request.Gender,
                Bio = request.Bio,
                City = request.City,
                Country = request.Country,
                LookingForGender = request.LookingForGender,
                UpdatedAt = DateTime.Now
            };
            _db.Profiles.Add(profile);
        }
        else
        {
            profile.KnownAs = request.KnownAs;
            profile.DateOfBirth = request.DateOfBirth;
            profile.Gender = request.Gender;
            profile.Bio = request.Bio;
            profile.City = request.City;
            profile.Country = request.Country;
            profile.LookingForGender = request.LookingForGender;
            profile.UpdatedAt = DateTime.Now;
        }

        await _db.SaveChangesAsync();

        return Ok(new { message = "Cập nhật hồ sơ thành công" });
    }

    [HttpGet("{userId:guid}")]
    public async Task<IActionResult> GetProfileByUserId([FromRoute] Guid userId)
    {
        var profile = await _db.Profiles.AsNoTracking().FirstOrDefaultAsync(p => p.UserId == userId);
        if (profile == null) return NotFound(new { message = "Không tìm thấy hồ sơ" });

        return Ok(new
        {
            id = profile.Id,
            userId = profile.UserId,
            knownAs = profile.KnownAs,
            dateOfBirth = profile.DateOfBirth,
            gender = profile.Gender,
            bio = profile.Bio,
            city = profile.City,
            country = profile.Country,
            lookingForGender = profile.LookingForGender,
            updatedAt = profile.UpdatedAt
        });
    }

    [HttpGet("suggestions")]
    [Authorize(Roles = "Customer")]
    public async Task<IActionResult> GetSuggestions([FromQuery] int take = 20)
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
                age = GetAge(p.DateOfBirth),
                lastActive = p.User.LastActive
            })
            .ToListAsync();

        return Ok(list);
    }

    private static int GetAge(DateOnly dob)
    {
        var today = DateOnly.FromDateTime(DateTime.Now);
        var age = today.Year - dob.Year;
        if (dob > today.AddYears(-age)) age--;
        return age;
    }
}


