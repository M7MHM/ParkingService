using MediatR;
using Parking.Application.Interfaces;
using Parking.Domain.Entities;
using Parking.Domain.Enums;
using Parking.Domain.Exceptions;
using Parking.Domain.ValueObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parking.Application.Features.Auth.Commands
{
    public record RegisterCommand(
        string Email,
        string Password,
        string FullName
    ) : IRequest<Guid>;
    public class RegisterCommandHandler : IRequestHandler<RegisterCommand, Guid>
    {
        private readonly IUserRepository _userRepository;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IUnitOfWork _unitOfWork;
        public RegisterCommandHandler(
            IUserRepository userRepository,
            IPasswordHasher passwordHasher,
            IUnitOfWork unitOfWork)
        {
            _userRepository = userRepository;
            _passwordHasher = passwordHasher;
            _unitOfWork = unitOfWork;
        }
        public async Task<Guid> Handle(RegisterCommand request, CancellationToken cancellationToken)
        {
            var existing = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);
            if (existing is not null)
                throw new EmailAlreadyRegisteredException(request.Email);

            var passwordHash = _passwordHasher.Hash(request.Password);

            var user = new User(
                new Email(request.Email),
                passwordHash,
                request.FullName,
                UserRole.Customer 
            );

            await _userRepository.AddAsync(user, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return user.Id;
        }
    }
}
