using CleanArchitectureDemo.Application.Interfaces;
using MediatR;

namespace CleanArchitectureDemo.Application.Commands.UpdateUser
{
    public record UpdateUserCommand(Guid Id, string Email) : IRequest<Unit>, ITransactionalCommand;
}
