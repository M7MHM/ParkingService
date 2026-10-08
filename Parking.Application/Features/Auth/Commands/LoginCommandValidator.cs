using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parking.Application.Features.Auth.Commands
{
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
