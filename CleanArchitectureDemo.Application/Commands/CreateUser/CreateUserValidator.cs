using System;
using System.Collections.Generic;
using System.Text;
using FluentValidation;

namespace CleanArchitectureDemo.Application.Commands.CreateUser;

public class CreateUserValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserValidator()
    {
        RuleFor(x => x.user.Email)
            .NotEmpty()
            .EmailAddress();
    }
}
