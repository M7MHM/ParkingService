using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Parking.Application.Features.Auth.Commands;

namespace Parking_Api.Controllers
{
    [ApiController]
    [Route("api/auth")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    public sealed class AuthController : ControllerBase
    {
        private readonly ISender _sender;
        public AuthController(ISender sender) => _sender = sender;

        [HttpPost("register")]
        public async Task<IActionResult> Register(
            [FromBody] RegisterCommand command,
            CancellationToken cancellationToken)
        {
            var userId = await _sender.Send(command, cancellationToken);
            return StatusCode(StatusCodes.Status201Created, new { userId });
        }
        [HttpPost("login")]
        public async Task<ActionResult<LoginResponse>> Login(
            [FromBody] LoginCommand command,
            CancellationToken cancellationToken)
        {
            var result = await _sender.Send(command, cancellationToken);
            return Ok(result);
        }
    }
}
