using Dating_Manager.DTOs;
using FluentValidation;
using Dating_Manager.DTOs;

namespace Dating_Manager.Validation;

public class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email là bắt buộc")
            .EmailAddress().WithMessage("Email không hợp lệ");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Mật khẩu là bắt buộc")
            .MinimumLength(6).WithMessage("Mật khẩu tối thiểu 6 ký tự");
    }
}

public class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email là bắt buộc")
            .EmailAddress().WithMessage("Email không hợp lệ");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Mật khẩu là bắt buộc");
    }
}

public class VerifyEmailRequestValidator : AbstractValidator<VerifyEmailRequest>
{
    public VerifyEmailRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email là bắt buộc")
            .EmailAddress().WithMessage("Email không hợp lệ");

        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Mã xác thực là bắt buộc")
            .Length(6).WithMessage("Mã xác thực phải gồm 6 ký tự");
    }
}

public class ForgotPasswordRequestValidator : AbstractValidator<ForgotPasswordRequest>
{
    public ForgotPasswordRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email là bắt buộc")
            .EmailAddress().WithMessage("Email không hợp lệ");
    }
}

public class ResetPasswordRequestValidator : AbstractValidator<ResetPasswordRequest>
{
    public ResetPasswordRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email là bắt buộc")
            .EmailAddress().WithMessage("Email không hợp lệ");

        RuleFor(x => x.Token)
            .NotEmpty().WithMessage("Token là bắt buộc");

        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage("Mật khẩu mới là bắt buộc")
            .MinimumLength(6).WithMessage("Mật khẩu mới tối thiểu 6 ký tự");
    }
}

public class UpdateStatusRequestValidator : AbstractValidator<UpdateStatusRequest>
{
    private static readonly string[] Allowed = new[] { "Active", "Hidden", "Inactive", "Deleted" };

    public UpdateStatusRequestValidator()
    {
        RuleFor(x => x.Status)
            .NotEmpty().WithMessage("Trạng thái là bắt buộc")
            .Must(s => Allowed.Contains(s))
            .WithMessage($"Trạng thái phải thuộc: {string.Join(", ", Allowed)}");
    }
}


