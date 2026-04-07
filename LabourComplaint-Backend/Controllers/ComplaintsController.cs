using FluentValidation;
using LabourComplaint_Backend.Features.Complaints.Dtos;
using LabourComplaint_Backend.Features.Complaints.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LabourComplaint_Backend.Features.Complaints.Controllers;

/// <summary>
/// Endpoints for managing labor complaint lifecycle: submission, tracking, and status updates.
/// All endpoints require authentication via JWT Bearer token.
/// </summary>
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

    /// <summary>
    /// Submit a new labor complaint
    /// </summary>
    /// <remarks>
    /// Creates a new complaint with optional initial evidence. 
    /// Returns a reference number (e.g., LC-2026-04-00842) for tracking.
    /// </remarks>
    /// <param name="dto">Complaint details including category, location, description</param>
    /// <param name="ct">Cancellation token</param>
    /// <response code="201">Complaint created successfully</response>
    /// <response code="400">Validation failed</response>
    /// <response code="401">Not authenticated</response>
    [HttpPost]
    [ProducesResponseType(typeof(ComplaintResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ComplaintResponseDto>> CreateComplaint(
        [FromBody] ComplaintCreateDto dto,
        CancellationToken ct)
    {
        var validationResult = await _validator.ValidateAsync(dto, ct);
        if (!validationResult.IsValid)
            return BadRequest(validationResult.ToDictionary());

        var userIdClaim = User.FindFirst("sub")
            ?? User.FindFirst("http://schemas.xmlsoap.org/ws/2005/identity/claims/nameidentifier");

        if (userIdClaim is null || !int.TryParse(userIdClaim.Value, out var userId))
            return Unauthorized();

        var result = await _service.CreateComplaintAsync(dto, userId, ct);

        // Fixed: Explicit generic type
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

    /// <summary>
    /// Get complaint details by reference number
    /// </summary>
    /// <remarks>
    /// Returns full complaint details including status, evidence, and chat preview.
    /// Users can only access complaints they own or are assigned to.
    /// </remarks>
    /// <param name="referenceNumber">Complaint reference (e.g., LC-2026-04-00842)</param>
    /// <param name="ct">Cancellation token</param>
    /// <response code="200">Complaint details returned</response>
    /// <response code="401">Not authenticated</response>
    /// <response code="403">Forbidden</response>
    /// <response code="404">Not found</response>
    [HttpGet("{referenceNumber}")]
    [ProducesResponseType(typeof(ComplaintResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ComplaintResponseDto>> GetComplaint(
        [FromRoute] string referenceNumber,
        CancellationToken ct)
    {
        var userIdClaim = User.FindFirst("sub")
            ?? User.FindFirst("http://schemas.xmlsoap.org/ws/2005/identity/claims/nameidentifier");

        if (userIdClaim is null || !int.TryParse(userIdClaim.Value, out var userId))
            return Unauthorized();

        var result = await _service.GetComplaintByReferenceAsync(referenceNumber, userId, ct);

        // Fixed: Correct Match logic for single complaint + explicit generic
        return result.Match<ActionResult<ComplaintResponseDto>>(
            onSuccess: complaint => Ok(complaint),  // Return the single complaint
            onFailure: error => NotFound(new { error })
        );
    }

    /// <summary>
    /// List complaints with optional filters
    /// </summary>
    /// <remarks>
    /// **Access Rules**:
    /// - Citizens: See only their own complaints
    /// - Inspectors: See complaints in their assigned district
    /// - Admins: See all complaints
    /// </remarks>
    /// <param name="districtId">Filter by district ID</param>
    /// <param name="status">Filter by status</param>
    /// <param name="page">Page number (default: 1)</param>
    /// <param name="limit">Items per page (default: 20)</param>
    /// <param name="ct">Cancellation token</param>
    /// <response code="200">Paginated list returned</response>
    /// <response code="401">Not authenticated</response>
    /// <response code="403">Forbidden</response>
    [HttpGet]
    // Fixed: Return type matches actual response (anonymous object)
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult> ListComplaints(  // Changed to ActionResult (no generic)
        [FromQuery] int? districtId,
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int limit = 20,
        CancellationToken ct = default)
    {
        var userIdClaim = User.FindFirst("sub");
        var userRole = User.FindFirst("role")?.Value;

        int? reporterId = null;
        if (userRole == "Citizen" && userIdClaim?.Value != null && int.TryParse(userIdClaim.Value, out var uid))
        {
            reporterId = uid;
        }

        // Note: ListComplaintsAsync should return Result<IEnumerable<ComplaintSummaryDto>>
        var result = await _service.ListComplaintsAsync(districtId, reporterId, status, page, limit, ct);

        // Fixed: Explicit generic type + correct variable names in scope
        return result.Match<ActionResult>(
            onSuccess: complaints =>  // Renamed 'responses' to 'complaints' for clarity
            {
                var pagination = new
                {
                    Page = page,      // Now in scope (method parameter)
                    Limit = limit,    // Now in scope
                    Total = complaints.Count()  // complaints is IEnumerable, use Count()
                };
                return Ok(new { data = complaints, pagination });
            },
            onFailure: error => Problem(detail: error)
        );
    }
}