using Dating_Manager.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.OData;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OData.ModelBuilder;
using Dating_Manager.Models;
using Dating_Manager.Services;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using FluentValidation;
using FluentValidation.AspNetCore;
using Dating_Manager.Validation;
using Microsoft.OpenApi.Models;
using Dating_Manager.Hubs;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionStr = builder.Configuration.GetConnectionString("MyCnn");
builder.Services.AddDbContext<DatingManagerContext>(options =>
    options.UseSqlServer(connectionStr));
builder.Services.AddScoped(typeof(DatingManagerContext));

//var modelBuilder = new ODataConventionModelBuilder();
//modelBuilder.EntitySet<AccountDTO>("Accounts");

builder.Services.AddControllers(options =>
{
    // Chuẩn hoá lỗi validate từ [ApiController]
})
    .AddOData(options => options
        //.AddRouteComponents("api", modelBuilder.GetEdmModel())
        .Filter()
        .Select()
        .OrderBy()
        .SetMaxTop(2)
        .Count()
    );

builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<RegisterRequestValidator>();

builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var errors = context.ModelState
            .Where(kvp => kvp.Value != null && kvp.Value.Errors.Count > 0)
            .ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value!.Errors.Select(e => string.IsNullOrWhiteSpace(e.ErrorMessage) ? "Invalid value" : e.ErrorMessage).ToArray()
            );

        var payload = new
        {
            message = "Dữ liệu không hợp lệ",
            errors
        };
        return new BadRequestObjectResult(payload);
    };
});
// JWT Bearer
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,

        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
    };

    // Cấu hình JWT cho SignalR
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;
            
            // Nếu request đến SignalR hub và có access_token trong query string
            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/chatHub"))
            {
                context.Token = accessToken;
            }
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization();
builder.Services.AddSingleton<JwtService>();
builder.Services.AddMemoryCache();
builder.Services.AddScoped<IEmailService, EmailService>();

// SignalR
builder.Services.AddSignalR();

// Đã thêm controllers ở trên cùng với OData và cấu hình validation

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Dating_Manager API",
        Version = "v1",
        Description = "API for Dating Manager project"
    });

    // 🟢 Định nghĩa lại phần xác thực JWT (để không cần gõ "Bearer ")
    c.AddSecurityDefinition("JWT", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,      // dùng HTTP thay vì ApiKey
        Scheme = "bearer",                   // swagger sẽ tự thêm Bearer
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Nhập token JWT (không cần thêm 'Bearer ' phía trước)"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "JWT"
                }
            },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        // Lưu ý: Khi dùng AllowCredentials(), không thể dùng AllowAnyOrigin()
        // Trong production, nên chỉ định origins cụ thể
        var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() 
            ?? new[] { "http://localhost:3000", "http://localhost:5173", "http://localhost:4200" };
        
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials(); // Cần thiết cho SignalR
    });
});


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Tạo thư mục uploads nếu chưa tồn tại
var uploadsPath = Path.Combine(builder.Environment.ContentRootPath, "wwwroot", "uploads");
if (!Directory.Exists(uploadsPath))
{
    Directory.CreateDirectory(uploadsPath);
}

// Serve static files for photos
app.UseStaticFiles();
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(uploadsPath),
    RequestPath = "/uploads"
});

app.UseCors("AllowAll");


// jwt bearer
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<ChatHub>("/chatHub");

// Seed role data nếu chưa có
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<DatingManagerContext>();
    if (!db.Roles.Any())
    {
        db.Roles.AddRange(
            new Role { Id = 1, Name = "Admin" },
            new Role { Id = 2, Name = "Customer" }
        );
        db.SaveChanges();
    }
}

app.Run();