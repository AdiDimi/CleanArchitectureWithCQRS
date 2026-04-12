using System;
using System.Collections.Generic;
using System.Text;

namespace CleanArchitectureDemo.Application.DTOs;

public record UserDto(
    string Id,
    string Email,
    int USER_ID,
    string USER_NAME
);