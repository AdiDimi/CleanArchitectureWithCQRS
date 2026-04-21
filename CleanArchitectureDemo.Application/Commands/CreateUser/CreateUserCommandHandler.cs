using CleanArchitectureDemo.Application.Exceptions;
using CleanArchitectureDemo.Application.Interfaces;
using CleanArchitectureDemo.Domain.Entities;
using MediatR;

namespace CleanArchitectureDemo.Application.Commands.CreateUser;

public class CreateUserCommandHandler : IRequestHandler<CreateUserCommand, string>
{
    private readonly IUserRepository _userRepository;

    public CreateUserCommandHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<string> Handle(CreateUserCommand request, CancellationToken ct)
    {
        // Check if user with email already exists
        if (await _userRepository.EmailExistsAsync(request.user.Email, ct))
        {
            throw new ConflictException("User", request.user.Email);
        }

        var user = request.user;
        await _userRepository.AddAsync(user, ct);

        return user.Id;
    }
}

public record DeleteUserCommand(Guid Id) 
    : IRequest<bool>, ITransactionalCommand;  // ← Add marker

public record UpdateUserCommand(Guid Id, string Email) 
    : IRequest<bool>, ITransactionalCommand;  // ← Add marker
