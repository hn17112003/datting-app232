using Dating_Manager.DTOs;
using FluentValidation;

namespace Dating_Manager.Validation;

public class UpdateProfileRequestValidator : AbstractValidator<UpdateProfileRequest>
{
    public UpdateProfileRequestValidator()
    {
        RuleFor(x => x.KnownAs)
            .NotEmpty().WithMessage("KnownAs là bắt buộc")
            .MaximumLength(100).WithMessage("KnownAs tối đa 100 ký tự");

        RuleFor(x => x.DateOfBirth)
            .NotEmpty().WithMessage("Ngày sinh là bắt buộc");

        RuleFor(x => x.Gender)
            .MaximumLength(50).When(x => x.Gender != null).WithMessage("Gender tối đa 50 ký tự");

        RuleFor(x => x.Bio)
            .MaximumLength(1500).When(x => x.Bio != null).WithMessage("Bio tối đa 1500 ký tự");

        RuleFor(x => x.City)
            .MaximumLength(100).When(x => x.City != null).WithMessage("City tối đa 100 ký tự");

        RuleFor(x => x.Country)
            .MaximumLength(100).When(x => x.Country != null).WithMessage("Country tối đa 100 ký tự");

        RuleFor(x => x.LookingForGender)
            .MaximumLength(50).When(x => x.LookingForGender != null).WithMessage("LookingForGender tối đa 50 ký tự");
    }
}


