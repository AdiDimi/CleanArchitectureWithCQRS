using CleanArchitectureDemo.Application.DTOs;
using CleanArchitectureDemo.Application.Interfaces;
using MediatR;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CleanArchitectureDemo.Application.Queries.GetAllUsers
{
    public class GetAllUsersHandler : IRequestHandler<GetAllUsersQuery, GetAllUsersResponse>
    {
        private readonly IUserRepository _userRepository;

        public GetAllUsersHandler(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task<GetAllUsersResponse> Handle(GetAllUsersQuery request, CancellationToken cancellationToken)
        {
            var users = await _userRepository.GetPagedAsync(
                request.PageNumber, 
                request.PageSize, 
                cancellationToken);

            var totalCount = await _userRepository.GetCountAsync(cancellationToken);
            var totalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize);

            var userDtos = users.Select(u => new UserDto(u.Id , u.Email,u.USER_ID,u.USER_NAME)).ToList();

            return new GetAllUsersResponse(
                userDtos,
                totalCount,
                request.PageNumber,
                request.PageSize,
                totalPages);
        }
    }
}
