// Features/Auth/Validators/RegisterDtoValidator.cs
using FluentValidation;
using LabourComplaint_Backend.Features.Auth.Dtos;
using LabourComplaint_Backend.Models.Enums;

namespace LabourComplaint_Backend.Features.Auth.Validators;
public class RegisterDtoValidator : AbstractValidator<RegisterDto>
{
    public RegisterDtoValidator()
    {
        RuleFor(x => x.Username)
            .NotEmpty().WithMessage("Username is required")
            .MinimumLength(3).MaximumLength(50);

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Invalid email format");

        RuleFor(x => x.Password)
            .NotEmpty()
            .MinimumLength(8).WithMessage("Password must be at least 8 characters")
            .Matches("[A-Z]").WithMessage("Password must contain an uppercase letter")
            .Matches("[a-z]").WithMessage("Password must contain a lowercase letter")
            .Matches("[0-9]").WithMessage("Password must contain a number");

        RuleFor(x => x.ConfirmPassword)
            .Equal(x => x.Password).WithMessage("Passwords do not match");

        RuleFor(x => x.Role).IsInEnum();

        // Inspectors must have district assigned
        RuleFor(x => x.AssignedDistrictId)
            .NotNull().When(x => x.Role == UserRole.Inspector)
            .WithMessage("Inspectors must be assigned to a district");
    }
}