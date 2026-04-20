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
        public static DynamicParameters GetUserByEmail(string email)
        {
            var parameters = new DynamicParameters();
            parameters.Add("p_email", OracleParameterFactory.Varchar2("p_email", email, 320));
            return parameters;
        }

        public static DynamicParameters InsertUser(string id, string email)
        {
            var parameters = new DynamicParameters();
            parameters.Add("p_id", OracleParameterFactory.Varchar2("p_id", id, 50));
            parameters.Add("p_email", OracleParameterFactory.Varchar2("p_email", email, 320));
            return parameters;
        }

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

        public static DynamicParameters GetPagedUsers(int offset, int pageSize){
            var parameters = new DynamicParameters();

            parameters.Add("p_offset", offset);
            parameters.Add("p_page_size", pageSize);

            //Create(("p_offset", offset), ("p_page_size", pageSize));
            //var cursorParameter = new OracleParameter
            //{
            //    ParameterName = "o_total_count",
            //    OracleDbType = OracleDbType.Int32,
            //    Direction = ParameterDirection.Output
            //};
            //parameters.Add("o_total_count", cursorParameter);
            return parameters;
        }

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
