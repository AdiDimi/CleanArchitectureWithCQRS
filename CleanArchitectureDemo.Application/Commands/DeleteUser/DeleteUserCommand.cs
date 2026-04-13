using CleanArchitectureDemo.Application.Interfaces;
using MediatR;
using System;

namespace CleanArchitectureDemo.Application.Commands.DeleteUser
{
    public record DeleteUserCommand(string Id) : IRequest<Unit>, ITransactionalCommand;
}
