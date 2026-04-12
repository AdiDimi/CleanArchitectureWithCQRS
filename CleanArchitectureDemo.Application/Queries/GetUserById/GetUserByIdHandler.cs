using CleanArchitectureDemo.Application.DTOs;
using CleanArchitectureDemo.Application.Exceptions;
using CleanArchitectureDemo.Application.Interfaces;
using MediatR;

namespace CleanArchitectureDemo.Application.Queries.GetUserById;

public class GetUserByIdHandler
    : IRequestHandler<GetUserByIdQuery, UserDto?>
{
    private readonly IUserRepository _userRepository;

    public GetUserByIdHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<UserDto?> Handle(GetUserByIdQuery request, CancellationToken ct)
    {
        var user = await _userRepository.GetByIdAsync(request.Id, ct);

        if (user == null)
        {
            throw new NotFoundException(nameof(Domain.Entities.User), request.Id);
        }

        return new UserDto(user.Id, user.Email,user.USER_ID,user.USER_NAME);
    }
}
