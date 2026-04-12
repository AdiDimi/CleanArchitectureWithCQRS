using CleanArchitectureDemo.Application.Interfaces;
using MediatR;
using System;

namespace CleanArchitectureDemo.Application.Commands.DeleteUser
{
    public record DeleteUserCommand(Guid Id) : IRequest<Unit>, ITransactionalCommand;
}
