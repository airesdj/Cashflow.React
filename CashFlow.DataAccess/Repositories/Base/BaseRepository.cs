using CashFlow.DataAccess.Mappers;
using log4net;

namespace CashFlow.DataAccess.Repositories.Base
{
    public class BaseRepository : IDisposable
    {
        private bool disposedValue;
        public ILog Log = null!;
        public DataAccessMapperFactory mapperFactory { get; set; }

        public BaseRepository()
        {
            mapperFactory = new DataAccessMapperFactory();
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
