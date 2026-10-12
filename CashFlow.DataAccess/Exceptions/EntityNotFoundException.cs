namespace CashFlow.DataAccess.Exceptions
{
    public class EntityNotFoundException : DataAccessException
    {
        public EntityNotFoundException(string message) : base(message) { }

        public EntityNotFoundException(string message, Exception e) : base(message, e)
        {
        }
    }
}
