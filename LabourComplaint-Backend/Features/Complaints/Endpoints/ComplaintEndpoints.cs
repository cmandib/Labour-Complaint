//using FluentValidation;
//using LabourComplaint_Backend.Features.Complaints.Dtos;
//using LabourComplaint_Backend.Features.Complaints.Services;
//using Microsoft.AspNetCore.Authorization;
//using Microsoft.AspNetCore.Mvc;
//using System.Security.Claims;

//namespace LabourComplaint_Backend.Features.Complaints.Endpoints;

///// <summary>
///// Endpoints for managing labor complaint lifecycle: submission, tracking, and status updates.
///// All endpoints require authentication via JWT Bearer token.
///// </summary>
//public static class ComplaintEndpoints
//{
//    /// <summary>
//    /// Registers all complaint-related endpoints under /api/complaints
//    /// </summary>
//    public static void MapComplaintEndpoints(this IEndpointRouteBuilder app)
//    {
//        var group = app.MapGroup("/api/complaints")
//            .WithTags("Complaints")
//            .RequireAuthorization(); // All endpoints require auth

//        // ─────────────────────────────────────────────────────────────
//        // POST /api/complaints - Submit new complaint
//        // ─────────────────────────────────────────────────────────────
//        /// <summary>
//        /// Submit a new labor complaint
//        /// </summary>
//        /// <remarks>
//        /// Creates a new complaint with optional initial evidence. 
//        /// Returns a reference number (e.g., LC-2026-04-00842) for tracking.
//        /// 
//        /// **Required Claims**: `sub` or `nameidentifier` (user ID)
//        /// </remarks>
//        /// <param name="dto">Complaint details including category, location, description</param>
//        /// <param name="validator">FluentValidation validator (auto-injected)</param>
//        /// <param name="service">Complaint service (auto-injected)</param>
//        /// <param name="user">Authenticated user claims (auto-injected)</param>
//        /// <param name="ct">Cancellation token</param>
//        /// <response code="201">Complaint created successfully with reference number</response>
//        /// <response code="400">Validation failed or business rule violation</response>
//        /// <response code="401">User not authenticated or invalid token</response>
//        group.MapPost("", async (
//            ComplaintCreateDto dto,
//            IValidator<ComplaintCreateDto> validator,
//            IComplaintService service,
//            ClaimsPrincipal user,
//            CancellationToken ct) =>
//        {
//            var validationResult = await validator.ValidateAsync(dto, ct);
//            if (!validationResult.IsValid)
//                return Results.ValidationProblem(validationResult.ToDictionary(),
//                    statusCode: StatusCodes.Status400BadRequest);

//            var userIdClaim = user.FindFirst("sub") ?? user.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier");
//            if (userIdClaim is null || !int.TryParse(userIdClaim.Value, out var userId))
//                return Results.Unauthorized();

//            var result = await service.CreateComplaintAsync(dto, userId, ct);

//            return result.Match(
//                onSuccess: response => Results.Created(
//                    $"/api/complaints/{response.ReferenceNumber}",
//                    response),
//                onFailure: error => Results.Problem(
//                    title: "Failed to create complaint",
//                    detail: error,
//                    statusCode: StatusCodes.Status400BadRequest)
//            );
//        })
//        .WithName("CreateComplaint")
//        .Produces<ComplaintResponseDto>(StatusCodes.Status201Created)
//        .ProducesProblem(StatusCodes.Status400BadRequest)
//        .ProducesProblem(StatusCodes.Status401Unauthorized)
//        .WithSummary("Submit a new labor complaint")
//        .WithDescription("Creates a new complaint with optional initial evidence. Returns reference number for tracking.");

//        // ─────────────────────────────────────────────────────────────
//        // GET /api/complaints/{referenceNumber} - Get complaint details
//        // ─────────────────────────────────────────────────────────────
//        /// <summary>
//        /// Get complaint details by reference number
//        /// </summary>
//        /// <remarks>
//        /// Returns full complaint details including status, evidence, and chat preview.
//        /// Users can only access complaints they own or are assigned to (district/role-based).
//        /// </remarks>
//        /// <param name="referenceNumber">Complaint reference (e.g., LC-2026-04-00842)</param>
//        /// <param name="service">Complaint service</param>
//        /// <param name="user">Authenticated user claims</param>
//        /// <param name="ct">Cancellation token</param>
//        /// <response code="200">Complaint details returned successfully</response>
//        /// <response code="401">User not authenticated</response>
//        /// <response code="403">User lacks permission to view this complaint</response>
//        /// <response code="404">Complaint not found</response>
//        group.MapGet("/{referenceNumber}", async (
//            string referenceNumber,
//            IComplaintService service,
//            ClaimsPrincipal user,
//            CancellationToken ct) =>
//        {
//            var userIdClaim = user.FindFirst("sub") ?? user.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier");
//            if (userIdClaim is null || !int.TryParse(userIdClaim.Value, out var userId))
//                return Results.Unauthorized();

//            var result = await service.GetComplaintByReferenceAsync(referenceNumber, userId, ct);

//            return result.Match(
//                onSuccess: Results.Ok,
//                onFailure: error => Results.NotFound(new { error })
//            );
//        })
//        .WithName("GetComplaint")
//        .Produces<ComplaintResponseDto>(StatusCodes.Status200OK)
//        .ProducesProblem(StatusCodes.Status401Unauthorized)
//        .ProducesProblem(StatusCodes.Status403Forbidden)
//        .ProducesProblem(StatusCodes.Status404NotFound)
//        .WithSummary("Get complaint by reference number")
//        .WithDescription("Returns full complaint details including status, evidence, and chat preview.");

//        // ─────────────────────────────────────────────────────────────
//        // GET /api/complaints - List complaints with filtering
//        // ─────────────────────────────────────────────────────────────
//        /// <summary>
//        /// List complaints with optional filters
//        /// </summary>
//        /// <remarks>
//        /// **Access Rules**:
//        /// - Citizens: See only their own complaints
//        /// - Inspectors: See complaints in their assigned district
//        /// - Admins: See all complaints
//        /// 
//        /// Supports pagination via `page` and `limit` query parameters.
//        /// </remarks>
//        /// <param name="service">Complaint service</param>
//        /// <param name="user">Authenticated user claims</param>
//        /// <param name="ct">Cancellation token</param>
//        /// <param name="districtId">Filter by district ID (inspectors/admins only)</param>
//        /// <param name="status">Filter by complaint status (e.g., Submitted, Assigned, Resolved)</param>
//        /// <param name="page">Page number (default: 1)</param>
//        /// <param name="limit">Items per page (default: 20, max: 100)</param>
//        /// <response code="200">Paginated list of complaints</response>
//        /// <response code="401">User not authenticated</response>
//        /// <response code="403">User lacks permission for requested filter</response>
//        group.MapGet("", async (
//            IComplaintService service,
//            ClaimsPrincipal user,
//            CancellationToken ct,
//            [FromQuery] int? districtId,
//            [FromQuery] string? status,
//            [FromQuery] int page = 1,
//            [FromQuery] int limit = 20) =>
//        {
//            var userIdClaim = user.FindFirst("sub");
//            var userRole = user.FindFirst("role")?.Value;

//            int? reporterId = null;
//            if (userRole == "Citizen" && userIdClaim?.Value != null && int.TryParse(userIdClaim.Value, out var uid))
//            {
//                reporterId = uid;
//            }

//            var result = await service.ListComplaintsAsync(districtId, reporterId, status, page, limit, ct);

//            return result.Match(
//                onSuccess: responses =>
//                {
//                    var pagination = new
//                    {
//                        Page = page,
//                        Limit = limit,
//                        Total = responses.Count // TODO: Replace with actual total count query in production
//                    };
//                    return Results.Ok(new { data = responses, pagination });
//                },
//                onFailure: error => Results.Problem(detail: error)
//            );
//        })
//        .WithName("ListComplaints")
//        .Produces<ComplaintResponseDto>(StatusCodes.Status200OK)
//        .ProducesProblem(StatusCodes.Status401Unauthorized)
//        .ProducesProblem(StatusCodes.Status403Forbidden)
//        .WithSummary("List complaints with filters")
//        .WithDescription("Returns paginated complaints filtered by district, status, or ownership. Access controlled by user role.");
//    }
//}