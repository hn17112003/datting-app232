using Dating_Manager.DTOs;
using FluentValidation;

namespace Dating_Manager.Validation;

public class SendMessageRequestValidator : AbstractValidator<SendMessageRequest>
{
    public SendMessageRequestValidator()
    {
        RuleFor(x => x.ConversationId)
            .NotEmpty().WithMessage("ConversationId là bắt buộc");

        RuleFor(x => x.RecipientId)
            .NotEmpty().WithMessage("RecipientId là bắt buộc");

        RuleFor(x => x.Content)
            .NotEmpty().WithMessage("Nội dung tin nhắn là bắt buộc");
    }
}

