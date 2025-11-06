using Dating_Manager.DTOs;
using FluentValidation;

namespace Dating_Manager.Validation;

public class CreateInterestRequestValidator : AbstractValidator<CreateInterestRequest>
{
    public CreateInterestRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Tên sở thích là bắt buộc")
            .MaximumLength(100).WithMessage("Tên tối đa 100 ký tự");
    }
}

public class UpdateUserInterestsRequestValidator : AbstractValidator<UpdateUserInterestsRequest>
{
    public UpdateUserInterestsRequestValidator()
    {
        RuleFor(x => x.InterestIds)
            .NotNull().WithMessage("Danh sách InterestIds là bắt buộc")
            .Must(list => list.Count > 0).WithMessage("Cần ít nhất 1 sở thích");
    }
}


