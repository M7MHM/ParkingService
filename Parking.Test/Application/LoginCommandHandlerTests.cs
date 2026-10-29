using FluentAssertions;
using Moq;
using Parking.Application.Features.Auth.Commands;
using Parking.Application.Interfaces;
using Parking.Domain.Entities;
using Parking.Domain.Enums;
using Parking.Domain.Exceptions;
using Parking.Test.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parking.Test.Application
{
    public sealed class LoginCommandHandlerTests
    {
        private readonly Mock<IUserRepository> _users = new(MockBehavior.Strict);
        private readonly Mock<IPasswordHasher> _hasher = new(MockBehavior.Strict);
        private readonly Mock<IJwtTokenGenerator> _jwt = new(MockBehavior.Strict);

        [Fact]
        public async Task Handle_ValidCredentials_ReturnsTokenAndUserInfo()
        {
            var user = TestData.CustomerUser();
            var command = new LoginCommand("user@example.com", "correct-password-123");

            _users.Setup(r => r.GetByEmailAsync(command.Email, It.IsAny<CancellationToken>())).ReturnsAsync(user);
            _hasher.Setup(h => h.Verify(command.Password, user.PasswordHash)).Returns(true);
            _jwt.Setup(g => g.GenerateToken(user)).Returns("jwt-token");
            _jwt.SetupGet(g => g.TokenLifetime).Returns(TimeSpan.FromMinutes(60));

            var response = await CreateHandler().Handle(command, CancellationToken.None);

            response.Token.Should().Be("jwt-token");
            response.UserId.Should().Be(user.Id);
            response.FullName.Should().Be(user.FullName);
            response.Role.Should().Be(UserRole.Customer);
            response.ExpiresAt.Should().BeCloseTo(DateTime.UtcNow.AddMinutes(60), TimeSpan.FromSeconds(5));
        }
        [Fact]
        public async Task Handle_UnknownEmail_ThrowsInvalidCredentialsAndNeverVerifiesPassword()
        {
            var command = new LoginCommand("nobody@example.com", "whatever-password-1");
            _users.Setup(r => r.GetByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
                .ReturnsAsync((User?)null);

            Func<Task> act = () => CreateHandler().Handle(command, CancellationToken.None);

            await act.Should().ThrowAsync<InvalidCredentialsException>();
            _hasher.Verify(h => h.Verify(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
            _jwt.Verify(g => g.GenerateToken(It.IsAny<User>()), Times.Never);
        }
        [Fact]
        public async Task Handle_WrongPassword_ThrowsInvalidCredentialsAndIssuesNoToken()
        {
            var user = TestData.CustomerUser();
            var command = new LoginCommand("user@example.com", "wrong-password-12345");
            _users.Setup(r => r.GetByEmailAsync(command.Email, It.IsAny<CancellationToken>())).ReturnsAsync(user);
            _hasher.Setup(h => h.Verify(command.Password, user.PasswordHash)).Returns(false);

            Func<Task> act = () => CreateHandler().Handle(command, CancellationToken.None);

            await act.Should().ThrowAsync<InvalidCredentialsException>();
            _jwt.Verify(g => g.GenerateToken(It.IsAny<User>()), Times.Never);
        }
        [Fact]
        public async Task Handle_UnknownEmailAndWrongPassword_ProduceIdenticalMessage()
        {
            var user = TestData.CustomerUser();
            _users.Setup(r => r.GetByEmailAsync("user@example.com", It.IsAny<CancellationToken>())).ReturnsAsync(user);
            _users.Setup(r => r.GetByEmailAsync("nobody@example.com", It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);
            _hasher.Setup(h => h.Verify(It.IsAny<string>(), user.PasswordHash)).Returns(false);

            var handler = CreateHandler();
            var unknown = await Record.ExceptionAsync(() =>
                handler.Handle(new LoginCommand("nobody@example.com", "x-password-1234567"), CancellationToken.None));
            var wrong = await Record.ExceptionAsync(() =>
                handler.Handle(new LoginCommand("user@example.com", "x-password-1234567"), CancellationToken.None));

            unknown.Should().BeOfType<InvalidCredentialsException>();
            wrong.Should().BeOfType<InvalidCredentialsException>();
            unknown!.Message.Should().Be(wrong!.Message);
        }

        private LoginCommandHandler CreateHandler() =>
            new(_users.Object, _hasher.Object, _jwt.Object);
    }
}
