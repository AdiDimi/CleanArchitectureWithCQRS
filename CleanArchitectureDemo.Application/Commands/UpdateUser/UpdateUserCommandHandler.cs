using CleanArchitectureDemo.Application.Exceptions;
using CleanArchitectureDemo.Application.Interfaces;
using CleanArchitectureDemo.Domain.Entities;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace CleanArchitectureDemo.Application.Commands.UpdateUser
{
    public class UpdateUserCommandHandler : IRequestHandler<UpdateUserCommand, Unit>
    {
        private readonly IUserRepository _userRepository;

        public UpdateUserCommandHandler(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task<Unit> Handle(UpdateUserCommand request, CancellationToken cancellationToken)
        {
            var user = await _userRepository.GetByIdAsync(request.Id, cancellationToken);

            if (user == null)
            {
                throw new NotFoundException(nameof(User), request.Id);
            }

            // Check if another user with the same email exists
            var existingUser = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);
            if (existingUser != null && existingUser.Id != request.Id.ToString()   )
            {
                throw new ConflictException("User", request.Email);
            }

            // Note: You would need to add an Update method to the User entity
            // For now, this demonstrates the repository pattern
            // In a real scenario, User entity should have domain methods to update its properties
            _userRepository.Update(user);

            return Unit.Value;
        }
    }
}
