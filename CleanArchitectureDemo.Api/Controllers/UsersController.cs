using CleanArchitectureDemo.Application.Commands.CreateUser;
using CleanArchitectureDemo.Application.Queries.GetAllUsers;
using CleanArchitectureDemo.Application.Queries.GetUserById;
using CleanArchitectureDemo.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CleanArchitectureDemo.Api.Controllers
{
    [ApiController]
    [Route("api/users")]
    public class UsersController : ControllerBase
    {
        private readonly IMediator _mediator;

        public UsersController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet]
        [ProducesResponseType(typeof(GetAllUsersResponse), StatusCodes.Status200OK)]
        public async Task<ActionResult<GetAllUsersResponse>> GetAll([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10, CancellationToken cancellationToken = default)
        {
            var response = await _mediator.Send(new GetAllUsersQuery(pageNumber, pageSize), cancellationToken);
            return Ok(response);
        }

        [HttpGet("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<User>> Get(string id, CancellationToken cancellationToken = default)
        {
            var user = await _mediator.Send(new GetUserByIdQuery(id), cancellationToken);
            return Ok(user);
        }

        //[HttpGet("{email}")]
        //[ProducesResponseType(StatusCodes.Status200OK)]
        //[ProducesResponseType(StatusCodes.Status404NotFound)]
        //public async Task<IActionResult> GetByEmail(string email, CancellationToken cancellationToken = default)
        //{
        //    var user = await _mediator.Send(new GetUserByEmailQuery(email), cancellationToken);
        //    return Ok(user);
        //}

        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Create(CreateUserCommand cmd, CancellationToken cancellationToken = default)
        {
            var id = await _mediator.Send(cmd, cancellationToken);
            return CreatedAtAction(nameof(Get), new { id }, new { id });
        }

        [HttpPut("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Update(string id, Application.Commands.UpdateUser.UpdateUserCommand cmd, CancellationToken cancellationToken = default)
        {
            if (id != cmd.Id)
            {
                return BadRequest("ID mismatch");
            }

            await _mediator.Send(cmd, cancellationToken);
            return NoContent();
        }

        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(string id, CancellationToken cancellationToken = default)
        {
            await _mediator.Send(new Application.Commands.DeleteUser.DeleteUserCommand(id), cancellationToken);
            return NoContent();
        }

 
    }
}
