using CashFlow.DataAccess.Mappers;
using Microsoft.Extensions.Logging;

namespace CashFlow.DataAccess.Repositories.Base
{
    public class LogHandler
    {
        public readonly ILogger<BaseRepository> logger;
        public LogHandler(ILogger<BaseRepository> logger)
        {
            this.logger = logger;
        }

        public void Error(Exception ex) => logger.LogError(ex, null);
        public void Error(string ex) => logger.LogError(ex);
        public void Info(string message) => logger.LogInformation(message);
        public void Debug(string message) => logger.LogDebug(message);
        public void Warning(string message) => logger.LogWarning(message);
    }

    public class BaseRepository : IDisposable
    {
        private bool disposedValue;
        public DataAccessMapperFactory mapperFactory { get; set; }
        public LogHandler Log { get; set; }

        public BaseRepository(ILogger<BaseRepository> logger)
        {
            mapperFactory = new DataAccessMapperFactory();
            Log = new LogHandler(logger);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    Log = null!;
                    mapperFactory?.Dispose();
                    mapperFactory = null!;
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        ~BaseRepository() { Dispose(disposing: false); }
    }
}
