// Features/Complaints/Controllers/ComplaintsController.cs
using System.Security.Claims;
using FluentValidation;
using LabourComplaint_Backend.Features.Complaints.Dtos;
using LabourComplaint_Backend.Features.Complaints.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LabourComplaint_Backend.Features.Complaints.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
[Tags("Complaints")]
public class ComplaintsController : ControllerBase
{
    private readonly IComplaintService _service;
    private readonly IValidator<ComplaintCreateDto> _validator;

    public ComplaintsController(
        IComplaintService service,
        IValidator<ComplaintCreateDto> validator)
    {
        _service = service;
        _validator = validator;
    }

    [HttpPost]
    [ProducesResponseType(typeof(ComplaintResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ComplaintResponseDto>> CreateComplaint(
        [FromBody] ComplaintCreateDto dto, CancellationToken ct)
    {
        var validationResult = await _validator.ValidateAsync(dto, ct);
        if (!validationResult.IsValid)
            return BadRequest(validationResult.ToDictionary());

        // 🔑 FIXED: Use same claim pattern as AuthController
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)
                       ?? User.FindFirst("sub");

        if (userIdClaim is null || !int.TryParse(userIdClaim.Value, out var userId))
            return Unauthorized();

        var result = await _service.CreateComplaintAsync(dto, userId, ct);

        return result.Match<ActionResult<ComplaintResponseDto>>(
            onSuccess: response => CreatedAtAction(
                nameof(GetComplaint),
                new { referenceNumber = response.ReferenceNumber },
                response),
            onFailure: error => Problem(
                title: "Failed to create complaint",
                detail: error,
                statusCode: StatusCodes.Status400BadRequest)
        );
    }

    [HttpGet("{referenceNumber}")]
    [ProducesResponseType(typeof(ComplaintResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ComplaintResponseDto>> GetComplaint(
        [FromRoute] string referenceNumber, CancellationToken ct)
    {
        // 🔑 FIXED: Same claim pattern
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)
                       ?? User.FindFirst("sub");

        if (userIdClaim is null || !int.TryParse(userIdClaim.Value, out var userId))
            return Unauthorized();

        var result = await _service.GetComplaintByReferenceAsync(referenceNumber, userId, ct);

        return result.Match<ActionResult<ComplaintResponseDto>>(
            onSuccess: complaint => Ok(complaint),
            onFailure: error => NotFound(new { error })
        );
    }

    [HttpGet]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult> ListComplaints(
        [FromQuery] int? districtId,
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int limit = 20,
        CancellationToken ct = default)
    {
        // 🔑 FIXED: Same claim pattern for role + user ID
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)
                       ?? User.FindFirst("sub");
        var userRole = User.FindFirst("role")?.Value;

        int? reporterId = null;
        if (userRole == "Citizen" && userIdClaim?.Value != null && int.TryParse(userIdClaim.Value, out var uid))
        {
            reporterId = uid;
        }

        var result = await _service.ListComplaintsAsync(districtId, reporterId, status, page, limit, ct);

        return result.Match<ActionResult>(
            onSuccess: paginated =>
            {
                var pagination = new PaginationMetadata
                {
                    Page = paginated.Page,
                    Limit = paginated.Limit,
                    Total = paginated.Total
                };

                return Ok(new ListComplaintsResponseDto
                {
                    Data = paginated.Items,
                    Pagination = pagination
                });
            },
            onFailure: error => Problem(detail: error)
        );
    }
}