namespace CashFlow.Core.Exceptions
{
    public class ServiceException : Exception
    {
        public ServiceException(String message) : base(message)
        {

        }

        public ServiceException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
