using CleanArchitectureDemo.Domain.Entities;
using Dapper;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Data;

namespace CleanArchitectureDemo.Infrastructure.Persistence.Parameters
{
    /// <summary>
    /// Strongly-typed parameters for User stored procedures and functions.
    /// Provides compile-time safety and prevents runtime parameter binding errors.
    /// </summary>
    public static class UserParameters
    {
        public static DynamicParameters GetUserByEmail(string email) => Create(("p_email", email));

     
        public static DynamicParameters InsertUser(string id, string email, int userId, string userName) =>
            Create(("p_id", id), ("p_email", email), ("p_user_id", userId), ("p_user_name", userName));
    

        public static DynamicParameters UpsertAudit(
            string userId,
            string payloadJson,
            DateTime changedAtUtc)
        {
            var parameters = new DynamicParameters();
            parameters.Add("p_user_id", OracleParameterFactory.Varchar2("p_user_id", userId, 50));
            parameters.Add("p_payload", OracleParameterFactory.Clob("p_payload", payloadJson));
            parameters.Add("p_changed_at", OracleParameterFactory.TimeStamp("p_changed_at", changedAtUtc));
            return parameters;
        }

        public static DynamicParameters GetUserById(string id) =>
            Create(("p_id", id));

        public static DynamicParameters GetPagedUsers(int offset, int pageSize) =>
            Create(("p_offset", offset), ("p_page_size", pageSize));
     
        public static DynamicParameters UpdateUser(string id, string email) =>
            Create(("p_id", id), ("p_email", email));

        public static DynamicParameters DeleteUser(string id) =>
            Create(("p_id", id));

        public static DynamicParameters UserExists(string id) =>
            Create(("p_id", id));

        public static DynamicParameters EmailExists(string email) =>
            Create(("p_email", email));

        private static DynamicParameters Create(params (string Name, object? Value)[] values)
        {
            var parameters = new DynamicParameters();

            foreach (var (name, value) in values)
            {
                parameters.Add(name, value);
            }

            return parameters;
        }
    }
}
