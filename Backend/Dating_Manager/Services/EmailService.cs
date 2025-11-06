using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace Dating_Manager.Services;

public interface IEmailService
{
    Task SendVerificationEmailAsync(string email, string verificationCode);
    Task SendPasswordResetEmailAsync(string email, string resetToken);
}

public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SendVerificationEmailAsync(string email, string verificationCode)
    {
        try
        {
            var smtpHost = _configuration["Email:SmtpHost"] ?? "smtp.gmail.com";
            var smtpPort = int.Parse(_configuration["Email:SmtpPort"] ?? "587");
            var smtpUser = _configuration["Email:SmtpUser"];
            var smtpPassword = _configuration["Email:SmtpPassword"];
            var fromEmail = _configuration["Email:FromEmail"] ?? smtpUser;
            var fromName = _configuration["Email:FromName"] ?? "Dating Manager";

            if (string.IsNullOrEmpty(smtpUser) || string.IsNullOrEmpty(smtpPassword))
            {
                _logger.LogWarning("Email configuration không đầy đủ. Sử dụng chế độ demo.");
                // Trong môi trường development, có thể log code thay vì gửi email
                _logger.LogInformation("Mã xác thực cho {Email}: {Code}", email, verificationCode);
                return;
            }

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(fromName, fromEmail));
            message.To.Add(new MailboxAddress("", email));
            message.Subject = "Xác thực email đăng ký tài khoản";

            var bodyBuilder = new BodyBuilder
            {
                HtmlBody = $@"
                    <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px;'>
                        <h2 style='color: #333;'>Xác thực email đăng ký</h2>
                        <p>Xin chào,</p>
                        <p>Cảm ơn bạn đã đăng ký tài khoản tại Dating Manager.</p>
                        <p>Mã xác thực của bạn là:</p>
                        <div style='background-color: #f4f4f4; padding: 20px; text-align: center; margin: 20px 0; border-radius: 5px;'>
                            <h1 style='color: #007bff; font-size: 32px; letter-spacing: 5px; margin: 0;'>{verificationCode}</h1>
                        </div>
                        <p>Mã này có hiệu lực trong 15 phút.</p>
                        <p>Nếu bạn không yêu cầu mã này, vui lòng bỏ qua email này.</p>
                        <p>Trân trọng,<br/>Đội ngũ Dating Manager</p>
                    </div>",
                TextBody = $"Mã xác thực email của bạn là: {verificationCode}. Mã này có hiệu lực trong 15 phút."
            };

            message.Body = bodyBuilder.ToMessageBody();

            using var client = new SmtpClient();
            await client.ConnectAsync(smtpHost, smtpPort, SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(smtpUser, smtpPassword);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);

            _logger.LogInformation("Email xác thực đã được gửi đến {Email}", email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi gửi email xác thực đến {Email}", email);
            // Không throw exception để không làm gián đoạn quá trình đăng ký
            // Trong production, có thể muốn throw hoặc xử lý khác
        }
    }

    public async Task SendPasswordResetEmailAsync(string email, string resetToken)
    {
        try
        {
            var smtpHost = _configuration["Email:SmtpHost"] ?? "smtp.gmail.com";
            var smtpPort = int.Parse(_configuration["Email:SmtpPort"] ?? "587");
            var smtpUser = _configuration["Email:SmtpUser"];
            var smtpPassword = _configuration["Email:SmtpPassword"];
            var fromEmail = _configuration["Email:FromEmail"] ?? smtpUser;
            var fromName = _configuration["Email:FromName"] ?? "Dating Manager";
            var baseUrl = _configuration["Email:ResetPasswordUrl"] ?? "http://localhost:5201/Auth/ResetPassword";
            // Mã hóa token để không hiển thị trực tiếp trong URL
            var encodedToken = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(resetToken));
            var resetUrl = $"{baseUrl}?t={Uri.EscapeDataString(encodedToken)}&e={Uri.EscapeDataString(Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(email)))}";

            if (string.IsNullOrEmpty(smtpUser) || string.IsNullOrEmpty(smtpPassword))
            {
                _logger.LogWarning("Email configuration không đầy đủ. Sử dụng chế độ demo.");
                _logger.LogInformation("Token đặt lại mật khẩu cho {Email}: {Token}", email, resetToken);
                return;
            }

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(fromName, fromEmail));
            message.To.Add(new MailboxAddress("", email));
            message.Subject = "Đặt lại mật khẩu";

            var bodyBuilder = new BodyBuilder
            {
                HtmlBody = $@"
                    <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px;'>
                        <h2 style='color: #333;'>Đặt lại mật khẩu</h2>
                        <p>Xin chào,</p>
                        <p>Bạn đã yêu cầu đặt lại mật khẩu cho tài khoản của mình.</p>
                        <p>Vui lòng click vào liên kết bên dưới để đặt lại mật khẩu:</p>
                        <div style='text-align: center; margin: 20px 0;'>
                            <a href='{resetUrl}' style='background-color: #007bff; color: white; padding: 12px 24px; text-decoration: none; border-radius: 5px; display: inline-block;'>Đặt lại mật khẩu</a>
                        </div>
                        <p>Hoặc copy và dán liên kết sau vào trình duyệt:</p>
                        <p style='word-break: break-all; color: #007bff;'>{resetUrl}</p>
                        <p>Liên kết này có hiệu lực trong 30 phút.</p>
                        <p>Nếu bạn không yêu cầu đặt lại mật khẩu, vui lòng bỏ qua email này.</p>
                        <p>Trân trọng,<br/>Đội ngũ Dating Manager</p>
                    </div>",
                TextBody = $"Để đặt lại mật khẩu, vui lòng truy cập: {resetUrl}. Liên kết này có hiệu lực trong 30 phút."
            };

            message.Body = bodyBuilder.ToMessageBody();

            using var client = new SmtpClient();
            await client.ConnectAsync(smtpHost, smtpPort, SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(smtpUser, smtpPassword);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);

            _logger.LogInformation("Email đặt lại mật khẩu đã được gửi đến {Email}", email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi gửi email đặt lại mật khẩu đến {Email}", email);
        }
    }
}

