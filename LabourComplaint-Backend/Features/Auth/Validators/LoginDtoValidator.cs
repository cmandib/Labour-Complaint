// Features/Auth/Validators/LoginDtoValidator.cs
using FluentValidation;

namespace LabourComplaint_Backend.Features.Auth.Validators;
public class LoginDtoValidator : AbstractValidator<LoginDto>
{
    public LoginDtoValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty();
    }
}