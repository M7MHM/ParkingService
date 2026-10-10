using FluentValidation;
using MediatR;
using Parking.Application.Interfaces;
using Parking.Domain.Enums;
using Parking.Domain.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parking.Application.Features.Auth.Commands
{
    public record LoginCommand(
           string Email,
           string Password
       ) : IRequest<LoginResponse>;
    public record LoginResponse(
        string Token,
        DateTime ExpiresAt,
        Guid UserId,
        string FullName,
        UserRole Role
    );
    public class LoginCommandHandler : IRequestHandler<LoginCommand, LoginResponse>
    {
        private readonly IUserRepository _userRepository;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IJwtTokenGenerator _jwtTokenGenerator;
        public LoginCommandHandler(
            IUserRepository userRepository,
            IPasswordHasher passwordHasher,
            IJwtTokenGenerator jwtTokenGenerator)
        {
            _userRepository = userRepository;
            _passwordHasher = passwordHasher;
            _jwtTokenGenerator = jwtTokenGenerator;
        }
        public async Task<LoginResponse> Handle(LoginCommand request, CancellationToken cancellationToken)
        {
            var user = await _userRepository.GetByEmailAsync(request.Email, cancellationToken)
                ?? throw new InvalidCredentialsException(); 

            if (!_passwordHasher.Verify(request.Password, user.PasswordHash))
                throw new InvalidCredentialsException();

            var token = _jwtTokenGenerator.GenerateToken(user);
            var expiresAt = DateTime.UtcNow.Add(_jwtTokenGenerator.TokenLifetime);

            return new LoginResponse(token, expiresAt, user.Id, user.FullName, user.Role);
        }
    }
    public sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
    {
        private const int MaximumPasswordLength = 128;
        public LoginCommandValidator()
        {
            RuleFor(command => command.Email)
                .Cascade(CascadeMode.Stop)
                .NotEmpty()
                .EmailAddress()
                .MaximumLength(255);

            RuleFor(command => command.Password)
                .Cascade(CascadeMode.Stop)
                .NotNull()
                .Must(password => !string.IsNullOrEmpty(password))
                .WithMessage("Password is required.")
                .Must(password => password is not null &&
                                  CountUnicodeCodePoints(password) <= MaximumPasswordLength)
                .WithMessage($"Password cannot contain more than {MaximumPasswordLength} characters.");
        }
        private static int CountUnicodeCodePoints(string value) =>
            value.EnumerateRunes().Count();
    }
}
