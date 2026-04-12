using MediatR;
using CleanArchitectureDemo.Application.DTOs;

namespace CleanArchitectureDemo.Application.Queries.GetUserById;

public record GetUserByIdQuery(Guid Id) : IRequest<UserDto?>;
