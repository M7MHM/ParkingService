using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Parking.Application.Features.Bookings.Commands;
using Parking.Application.Features.Bookings.Queries;
using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Parking_Api.Controllers
{
    [ApiController]
    [Route("api/bookings")]
    [Authorize]
    public sealed class BookingsController : ControllerBase
    {
        private readonly ISender _sender;
        public BookingsController(ISender sender) => _sender = sender;

        [HttpGet("{id:guid}")]
        public async Task<ActionResult<BookingResponse>> GetById(Guid id, CancellationToken ct)
        {
            var result = await _sender.Send(new GetBookingByIdQuery(id), ct);
            if (result is null)
                return NotFound();

            if (!User.IsInRole("Admin") &&
                (!TryGetUserId(out var callerId) || result.UserId != callerId))
                return NotFound();

            return Ok(result);
        }
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateBookingRequest request, CancellationToken ct)
        {
            if (!TryGetUserId(out var userId))
                return Unauthorized();

            var command = new CreateBookingCommand(
                request.ParkingLotId!.Value,
                request.ParkingSpotId!.Value,
                userId, // Always use the signed token's sub, never a body-supplied user ID.
                request.StartTime!.Value.UtcDateTime,
                request.EndTime!.Value.UtcDateTime,
                request.Notes);

            var id = await _sender.Send(command, ct);
            return CreatedAtAction(nameof(GetById), new { id }, new { id });
        }
        [HttpPost("{id:guid}/cancel")]
        public async Task<IActionResult> Cancel(Guid id, CancellationToken ct)
        {
            if (!TryGetUserId(out var userId))
                return Unauthorized();

            await _sender.Send(new CancelBookingCommand(id, userId, User.IsInRole("Admin")), ct);
            return NoContent();
        }
        [HttpPost("{id:guid}/complete")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Complete(Guid id, CancellationToken ct)
        {
            await _sender.Send(new CompleteBookingCommand(id), ct);
            return NoContent();
        }
        private bool TryGetUserId(out Guid userId) =>
            Guid.TryParse(User.FindFirstValue(JwtRegisteredClaimNames.Sub), out userId);
    }
    public sealed record CreateBookingRequest(
        [property: Required] Guid? ParkingLotId,
        [property: Required] Guid? ParkingSpotId,
        [property: Required] DateTimeOffset? StartTime,
        [property: Required] DateTimeOffset? EndTime,
        string? Notes = null
    );
}
