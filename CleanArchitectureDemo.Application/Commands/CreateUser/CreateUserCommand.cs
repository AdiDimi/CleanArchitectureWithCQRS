using CleanArchitectureDemo.Application.Interfaces;
using CleanArchitectureDemo.Domain.Entities;
using MediatR;

namespace CleanArchitectureDemo.Application.Commands.CreateUser;

public record CreateUserCommand(string Email) : IRequest<Guid>, ITransactionalCommand;
