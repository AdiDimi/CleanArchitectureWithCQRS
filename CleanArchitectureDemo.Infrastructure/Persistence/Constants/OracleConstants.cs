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
        public const string GetUserById = "PKG_USERS.GET_BY_ID";
        public const string GetUserByEmail = "PKG_USERS.GET_BY_EMAIL";
        public const string GetAllUsers = "PKG_USERS.GET_ALL";
        public const string GetPagedUsers = "PKG_USERS.GET_PAGED";
        public const string UserExists = "PKG_USERS.USER_EXISTS";
        public const string EmailExists = "PKG_USERS.EMAIL_EXISTS";
        public const string GetUserCount = "PKG_USERS.GET_USER_COUNT";

        // User procedures (commands)
        public const string InsertUser = "PKG_USERS.INSERT_USER";
        public const string UpdateUser = "PKG_USERS.UPDATE_USER";
        public const string DeleteUser = "PKG_USERS.DELETE_USER";
    }

 
}
