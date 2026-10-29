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
    public sealed class RegisterCommandHandlerTests
    {
        private readonly Mock<IUserRepository> _users = new(MockBehavior.Strict);
        private readonly Mock<IPasswordHasher> _hasher = new(MockBehavior.Strict);
        private readonly Mock<IUnitOfWork> _unitOfWork = new(MockBehavior.Strict);

        [Fact]
        public async Task Handle_NewEmail_CreatesCustomerWithHashedPasswordAndNormalizedEmail()
        {
            var command = new RegisterCommand("User@Example.COM", "a-long-enough-password", "Test User");
            User? added = null;

            _users.Setup(r => r.GetByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
                .ReturnsAsync((User?)null);
            _hasher.Setup(h => h.Hash(command.Password)).Returns("hashed-value");
            _users.Setup(r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
                .Callback<User, CancellationToken>((u, _) => added = u)
                .Returns(Task.CompletedTask);
            _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

            var id = await CreateHandler().Handle(command, CancellationToken.None);

            added.Should().NotBeNull();
            id.Should().Be(added!.Id);
            added.Email.Value.Should().Be("user@example.com");
            added.PasswordHash.Should().Be("hashed-value");
            added.PasswordHash.Should().NotBe(command.Password);
            added.Role.Should().Be(UserRole.Customer);
            _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }
        [Fact]
        public async Task Handle_EmailAlreadyRegistered_ThrowsAndDoesNotHashOrSave()
        {
            var command = new RegisterCommand("user@example.com", "a-long-enough-password", "Test User");
            _users.Setup(r => r.GetByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
                .ReturnsAsync(TestData.CustomerUser());

            Func<Task> act = () => CreateHandler().Handle(command, CancellationToken.None);

            await act.Should().ThrowAsync<EmailAlreadyRegisteredException>();
            _hasher.Verify(h => h.Hash(It.IsAny<string>()), Times.Never);
            _users.Verify(r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
            _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }
        private RegisterCommandHandler CreateHandler() =>
            new(_users.Object, _hasher.Object, _unitOfWork.Object);
    }

}
