using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parking.Application.Features.Auth.Commands
{
    public sealed class RegisterCommandValidator : AbstractValidator<RegisterCommand>
    {
        private const int MinimumPasswordLength = 15;
        private const int MaximumPasswordLength = 128;
        public RegisterCommandValidator()
        {
            RuleFor(command => command.Email)
                .Cascade(CascadeMode.Stop)
                .NotEmpty()
                .EmailAddress()
                .MaximumLength(255);

            RuleFor(command => command.Password)
                .Cascade(CascadeMode.Stop)
                .NotNull()
                .Must(password => password is not null &&
                                  CountUnicodeCodePoints(password) >= MinimumPasswordLength)
                .WithMessage($"Password must contain at least {MinimumPasswordLength} characters.")
                .Must(password => password is not null &&
                                  CountUnicodeCodePoints(password) <= MaximumPasswordLength)
                .WithMessage($"Password cannot contain more than {MaximumPasswordLength} characters.");

            RuleFor(command => command.FullName)
                .Cascade(CascadeMode.Stop)
                .NotEmpty()
                .MaximumLength(150);
        }
        private static int CountUnicodeCodePoints(string value) =>
            value.EnumerateRunes().Count();
    }
}
