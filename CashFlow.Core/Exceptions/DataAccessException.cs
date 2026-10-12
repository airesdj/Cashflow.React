namespace CashFlow.Core.Exceptions
{
    public class DataAccessException : Exception
    {
        public DataAccessException(String message, Exception e) : base(message, e)
        {

        }

        public DataAccessException(String message) : base(message)
        {

        }
    }
}
