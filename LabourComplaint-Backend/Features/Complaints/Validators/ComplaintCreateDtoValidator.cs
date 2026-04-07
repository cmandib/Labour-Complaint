using FluentValidation;
using LabourComplaint_Backend.Data;
using LabourComplaint_Backend.Features.Complaints.Dtos;
using Microsoft.EntityFrameworkCore;

namespace LabourComplaint_Backend.Features.Complaints.Validators;

public class ComplaintCreateDtoValidator : AbstractValidator<ComplaintCreateDto>
{
    public ComplaintCreateDtoValidator(ApplicationDbContext db)
    {
        RuleFor(x => x.DistrictId)
            .NotEmpty().WithMessage("District is required")
            .MustAsync(async (id, ct) =>
                await db.Districts.AnyAsync(d => d.Id == id && !d.IsDeleted, ct))
            .WithMessage("Invalid district selected");

        RuleFor(x => x.Category)
            .NotEmpty()
            .MaximumLength(100)
            .Must(BeValidCategory).WithMessage("Invalid complaint category");

        RuleFor(x => x.Description)
            .NotEmpty()
            .MinimumLength(10).WithMessage("Description must be at least 10 characters")
            .MaximumLength(2000);

        RuleFor(x => x.WorkplaceName)
            .MaximumLength(200).When(x => !string.IsNullOrWhiteSpace(x.WorkplaceName));

        RuleFor(x => x.LocationAddress)
            .MaximumLength(300).When(x => !string.IsNullOrWhiteSpace(x.LocationAddress));

        // GPS coordinates must both be present or both absent
        RuleFor(x => x.GpsLat)
            .Must((dto, lat) =>
                (dto.GpsLat.HasValue && dto.GpsLng.HasValue) ||
                (!dto.GpsLat.HasValue && !dto.GpsLng.HasValue))
            .WithMessage("Both latitude and longitude must be provided together");

        RuleFor(x => x.Severity)
            .InclusiveBetween(1, 4).WithMessage("Severity must be between 1 (Low) and 4 (Urgent)");

        RuleFor(x => x.ConfidentialityLevel)
            .InclusiveBetween(1, 3).WithMessage("Confidentiality level must be 1 (Public), 2 (Inspector), or 3 (Admin)");

        RuleFor(x => x.InitialEvidence)
            .Must(e => e == null || e.Count <= 10).WithMessage("Maximum 10 evidence files allowed at submission")
            .When(x => x.InitialEvidence != null);
    }

    private bool BeValidCategory(string category)
    {
        // In production, load valid categories from config/database
        var validCategories = new[]
        {
            "Wage Theft", "Safety Violation", "Harassment",
            "Discrimination", "Contract Violation", "Other"
        };
        return validCategories.Contains(category);
    }
}