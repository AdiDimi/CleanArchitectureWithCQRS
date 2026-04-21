using CleanArchitectureDemo.Application.Interfaces;
using CleanArchitectureDemo.Domain.Entities;
using MediatR;

namespace CleanArchitectureDemo.Application.Commands.CreateUser;

public record CreateUserCommand(User user) : IRequest<string>, ITransactionalCommand;
