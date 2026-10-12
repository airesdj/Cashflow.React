namespace CashFlow.DataAccess.Exceptions
{
    public class DataAccessException : Exception
    {
        public DataAccessException(string message, Exception e) : base(message, e)
        {

        }

        public DataAccessException(string message) : base(message)
        {

        }
    }
}
