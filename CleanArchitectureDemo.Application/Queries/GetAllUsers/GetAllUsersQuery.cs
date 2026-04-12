using CleanArchitectureDemo.Application.DTOs;
using MediatR;
using System.Collections.Generic;

namespace CleanArchitectureDemo.Application.Queries.GetAllUsers
{
    public record GetAllUsersQuery(int PageNumber = 1, int PageSize = 10) : IRequest<GetAllUsersResponse>;

    public record GetAllUsersResponse(
        IReadOnlyList<UserDto> Users,
        int TotalCount,
        int PageNumber,
        int PageSize,
        int TotalPages);
}
