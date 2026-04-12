namespace CleanArchitectureDemo.Application.Interfaces
{
    /// <summary>
    /// Marker interface to indicate that a command requires transaction management.
    /// Commands implementing this interface will be wrapped in a database transaction.
    /// </summary>
    public interface ITransactionalCommand
    {
    }
}
