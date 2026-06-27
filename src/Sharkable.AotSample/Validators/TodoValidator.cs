using FluentValidation;

namespace Sharkable.AotSample;

/// <summary>
/// Validates <see cref="CreateTodoRequest"/> and <see cref="UpdateTodoRequest"/>.
/// </summary>
public sealed class TodoValidator : AbstractValidator<CreateTodoRequest>
{
    public TodoValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required")
            .MaximumLength(200).WithMessage("Title must be at most 200 characters");

        RuleFor(x => x.Description)
            .MaximumLength(2000).When(x => x.Description is not null)
            .WithMessage("Description must be at most 2000 characters");
    }
}
