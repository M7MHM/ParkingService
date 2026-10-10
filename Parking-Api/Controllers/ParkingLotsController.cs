using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Parking.Application.Features.Parkings.Commands;
using Parking.Application.Features.Parkings.Queries;
using ParkingBooking.Domain.Enums;
using ParkingBooking.Domain.ValueObjects;
using System.ComponentModel.DataAnnotations;

namespace Parking_Api.Controllers
{
    [ApiController]
    [Route("api/parking-lots")]
    public sealed class ParkingLotsController : ControllerBase
    {
        private readonly ISender _sender;
        public ParkingLotsController(ISender sender) => _sender = sender;

        [HttpGet("{id:guid}")]
        [AllowAnonymous]
        public async Task<ActionResult<ParkingLotResponse>> GetById(Guid id, CancellationToken ct)
        {
            var result = await _sender.Send(new GetParkingLotByIdQuery(id), ct);
            return result is null ? NotFound() : Ok(result);
        }
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([FromBody] CreateParkingLotRequest request, CancellationToken ct)
        {
            var command = new CreateParkingLotCommand(
                request.Name,
                request.Description,
                new Location(request.Latitude!.Value, request.Longitude!.Value, request.Address),
                new Money(request.HourlyRateAmount!.Value, request.Currency),
                request.TotalSpots!.Value,
                request.OpeningTime!.Value,
                request.ClosingTime!.Value);

            var id = await _sender.Send(command, ct);
            return CreatedAtAction(nameof(GetById), new { id }, new { id });
        }
        [HttpGet("{parkingLotId:guid}/spots/available")]
        [AllowAnonymous]
        public async Task<ActionResult<IReadOnlyList<ParkingSpotResponse>>> GetAvailableSpots(
            Guid parkingLotId,
            CancellationToken ct)
        {
            var spots = await _sender.Send(new GetAvailableSpotsQuery(parkingLotId), ct);
            return Ok(spots);
        }

        [HttpPost("{parkingLotId:guid}/spots")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AddSpot(
            Guid parkingLotId,
            [FromBody] AddSpotRequest request,
            CancellationToken ct)
        {
            var command = new AddSpotCommand(
                parkingLotId,
                request.SpotNumber,
                new SpotType(request.Size!.Value, request.HasElectricCharger, request.IsCovered, request.IsAccessible),
                new Money(request.HourlyRateAmount!.Value, request.Currency));

            var spotId = await _sender.Send(command, ct);
            return StatusCode(StatusCodes.Status201Created, new { spotId, parkingLotId });
        }
    }
    public sealed record CreateParkingLotRequest(
        string Name,
        string Description,
        [property: Required] double? Latitude,
        [property: Required] double? Longitude,
        string? Address,
        [property: Required] decimal? HourlyRateAmount,
        [property: Required] int? TotalSpots,
        [property: Required] TimeSpan? OpeningTime,
        [property: Required] TimeSpan? ClosingTime,
        string Currency = "EGP");

    public sealed record AddSpotRequest(
        string SpotNumber,
        [property: Required] SpotSize? Size,
        [property: Required] decimal? HourlyRateAmount,
        string Currency = "EGP",
        bool HasElectricCharger = false,
        bool IsCovered = false,
        bool IsAccessible = false);
}
