using System;
using System.Collections.Generic;
using System.Text;

namespace CleanArchitectureDemo.Domain.Entities;

public class User
{
    public string Id { get; set; }
    public string Email { get; set; } = string.Empty;

    public int USER_ID { get; set; }

    public string USER_NAME { get; set; }
    // Parameterless constructor for Dapper
    public User() 
    {
    }

    // Constructor for domain logic
    public User(string email)
    {
        Id = Guid.NewGuid().ToString();
        //USER_ID = int.Parse(Id);
        Email = email;
    }
}
