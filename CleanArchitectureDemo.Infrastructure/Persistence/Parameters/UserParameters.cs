using System;
using System.Collections.Generic;
using Dapper;

namespace CleanArchitectureDemo.Infrastructure.Persistence.Parameters
{
    /// <summary>
    /// Strongly-typed parameters for User stored procedures and functions.
    /// Provides compile-time safety and prevents runtime parameter binding errors.
    /// </summary>
    public static class UserParameters
    {
        public static DynamicParameters GetUserById(Guid id)
        {
            var parameters = new DynamicParameters();
            parameters.Add("p_id", id.ToString());
            return parameters;
        }

        public static DynamicParameters GetUserByEmail(string email)
        {
            var parameters = new DynamicParameters();
            parameters.Add("p_email", email);
            return parameters;
        }

        public static DynamicParameters GetPagedUsers(int offset, int pageSize)
        {
            var parameters = new DynamicParameters();
            parameters.Add("p_offset", offset);
            parameters.Add("p_page_size", pageSize);
            return parameters;
        }

        public static DynamicParameters InsertUser(string id, string email)
        {
            var parameters = new DynamicParameters();
            parameters.Add("p_id", id);
            parameters.Add("p_email", email);
            return parameters;
        }

        public static DynamicParameters UpdateUser(string id, string email)
        {
            var parameters = new DynamicParameters();
            parameters.Add("p_id", id);
            parameters.Add("p_email", email);
            return parameters;
        }

        public static DynamicParameters DeleteUser(string id)
        {
            var parameters = new DynamicParameters();
            parameters.Add("p_id", id);
            return parameters;
        }

        public static DynamicParameters UserExists(Guid id)
        {
            var parameters = new DynamicParameters();
            parameters.Add("p_id", id.ToString());
            return parameters;
        }

        public static DynamicParameters EmailExists(string email)
        {
            var parameters = new DynamicParameters();
            parameters.Add("p_email", email);
            return parameters;
        }
    }
}
