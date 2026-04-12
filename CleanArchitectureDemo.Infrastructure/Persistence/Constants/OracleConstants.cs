namespace CleanArchitectureDemo.Infrastructure.Persistence.Constants
{
    /// <summary>
    /// Oracle stored procedure and function parameter names.
    /// Provides compile-time safety and prevents typos.
    /// </summary>
    public static class OracleParameters
    {
        // User-related parameters
        public const string UserId = "p_id";
        public const string UserEmail = "p_email";
        public const string Offset = "p_offset";
        public const string PageSize = "p_page_size";

        // Add more parameters as needed for other entities
        // Order-related parameters
        public const string OrderId = "p_order_id";
        public const string CustomerId = "p_customer_id";
        public const string OrderTotal = "p_total";
        public const string OrderStatus = "p_status";
    }

    /// <summary>
    /// Oracle stored procedure and function names.
    /// Provides compile-time safety and prevents typos.
    /// </summary>
    public static class OracleProcedures
    {
        // User functions (queries)
        public const string GetUserById = "FN_GET_USER_BY_ID";
        public const string GetUserByEmail = "FN_GET_USER_BY_EMAIL";
        public const string GetAllUsers = "FN_GET_ALL_USERS";
        public const string GetPagedUsers = "FN_GET_PAGED_USERS";
        public const string UserExists = "FN_USER_EXISTS";
        public const string EmailExists = "FN_EMAIL_EXISTS";
        public const string GetUserCount = "FN_GET_USER_COUNT";

        // User procedures (commands)
        public const string InsertUser = "SP_INSERT_USER";
        public const string UpdateUser = "SP_UPDATE_USER";
        public const string DeleteUser = "SP_DELETE_USER";
    }
}
