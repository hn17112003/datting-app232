using System.Security.Claims;
using Dating_Manager.DTOs;
using Dating_Manager.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;

namespace Dating_Manager.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PhotosController : ControllerBase
{
    private readonly DatingManagerContext _db;
    private readonly IWebHostEnvironment _env;

    public PhotosController(DatingManagerContext db, IWebHostEnvironment env)
    {
        _db = db;
        _env = env;
    }

    // POST /api/photos - Upload (demo: giả lập upload, lưu metadata)
    [HttpPost]
    [Consumes("multipart/form-data")]
    [Authorize(Roles = "Customer")]
    public async Task<IActionResult> Upload([FromForm] PhotoDtos dto)
    {
        if (dto.File == null || dto.File.Length == 0)
        {
            return BadRequest(new { message = "File không hợp lệ" });
        }

        var email = User.FindFirstValue(ClaimTypes.Name);
        if (string.IsNullOrEmpty(email)) return Unauthorized();

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (user == null) return Unauthorized();

        var profile = await _db.Profiles.FirstOrDefaultAsync(p => p.UserId == user.Id);
        if (profile == null)
        {
            return BadRequest(new { message = "Bạn cần tạo hồ sơ trước khi tải ảnh" });
        }

        // Tạo thư mục uploads nếu chưa có
        var uploadsFolder = Path.Combine(_env.ContentRootPath, "wwwroot", "uploads");
        if (!Directory.Exists(uploadsFolder))
        {
            Directory.CreateDirectory(uploadsFolder);
        }

        // Tạo tên file unique
        var fileExtension = Path.GetExtension(dto.File.FileName);
        var fileName = $"{Guid.NewGuid()}{fileExtension}";
        var filePath = Path.Combine(uploadsFolder, fileName);

        // Lưu file
        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await dto.File.CopyToAsync(stream);
        }

        // Tạo URL để truy cập file
        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        var url = $"{baseUrl}/uploads/{fileName}";

        var hasAny = await _db.Photos.AnyAsync(ph => ph.ProfileId == profile.Id);

        var photo = new Photo
        {
            Id = Guid.NewGuid(),
            ProfileId = profile.Id,
            Url = url,
            PublicId = fileName,
            PhotoType = string.IsNullOrWhiteSpace(dto.PhotoType) ? null : dto.PhotoType,
            IsMain = !hasAny,
            UploadedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };

        _db.Photos.Add(photo);
        await _db.SaveChangesAsync();

        return Ok(new
        {
            id = photo.Id,
            url = photo.Url,
            isMain = photo.IsMain,
            photoType = photo.PhotoType
        });
    }

    // GET /api/photos/{userId}
    [HttpGet("{userId:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetByUser([FromRoute] Guid userId)
    {
        var profile = await _db.Profiles.AsNoTracking().FirstOrDefaultAsync(p => p.UserId == userId);
        if (profile == null) return NotFound(new { message = "Không tìm thấy hồ sơ" });

        var photos = await _db.Photos.AsNoTracking()
            .Where(ph => ph.ProfileId == profile.Id)
            .OrderByDescending(ph => ph.IsMain)
            .ThenByDescending(ph => ph.UploadedAt)
            .Select(ph => new
            {
                id = ph.Id,
                url = ph.Url,
                isMain = ph.IsMain,
                photoType = ph.PhotoType,
                uploadedAt = ph.UploadedAt
            })
            .ToListAsync();

        return Ok(photos);
    }

    // PUT /api/photos/{photoId}/set-main
    [HttpPut("{photoId:guid}/set-main")]
    [Authorize(Roles = "Customer")]
    public async Task<IActionResult> SetMain([FromRoute] Guid photoId)
    {
        var email = User.FindFirstValue(ClaimTypes.Name);
        if (string.IsNullOrEmpty(email)) return Unauthorized();

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (user == null) return Unauthorized();

        var photo = await _db.Photos.Include(ph => ph.Profile).FirstOrDefaultAsync(ph => ph.Id == photoId);
        if (photo == null) return NotFound(new { message = "Không tìm thấy ảnh" });

        if (photo.Profile.UserId != user.Id)
        {
            return Forbid();
        }

        var profileId = photo.ProfileId;

        var photosSameProfile = await _db.Photos.Where(ph => ph.ProfileId == profileId).ToListAsync();
        foreach (var p in photosSameProfile)
        {
            p.IsMain = false;
        }
        photo.IsMain = true;
        photo.UpdatedAt = DateTime.Now;

        await _db.SaveChangesAsync();
        return Ok(new { message = "Đặt ảnh chính thành công" });
    }

    // DELETE /api/photos/{photoId}
    [HttpDelete("{photoId:guid}")]
    [Authorize(Roles = "Customer")]
    public async Task<IActionResult> Delete([FromRoute] Guid photoId)
    {
        var email = User.FindFirstValue(ClaimTypes.Name);
        if (string.IsNullOrEmpty(email)) return Unauthorized();

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (user == null) return Unauthorized();

        var photo = await _db.Photos.Include(ph => ph.Profile).FirstOrDefaultAsync(ph => ph.Id == photoId);
        if (photo == null) return NotFound(new { message = "Không tìm thấy ảnh" });

        if (photo.Profile.UserId != user.Id)
        {
            return Forbid();
        }

        var wasMain = photo.IsMain;
        var profileId = photo.ProfileId;

        _db.Photos.Remove(photo);
        await _db.SaveChangesAsync();

        if (wasMain)
        {
            var next = await _db.Photos.Where(p => p.ProfileId == profileId).OrderByDescending(p => p.UploadedAt).FirstOrDefaultAsync();
            if (next != null)
            {
                next.IsMain = true;
                next.UpdatedAt = DateTime.Now;
                await _db.SaveChangesAsync();
            }
        }

        // Thay bằng gọi xoá Cloudinary/S3 theo PublicId nếu đã tích hợp.

        return NoContent();
    }
}


