using Dating_Manager.DTOs;
using FluentValidation;

namespace Dating_Manager.Validation;

public class CreateConversationRequestValidator : AbstractValidator<CreateConversationRequest>
{
    public CreateConversationRequestValidator()
    {
        RuleFor(x => x.User2Id)
            .NotEmpty().WithMessage("User2Id là bắt buộc");
    }
}

