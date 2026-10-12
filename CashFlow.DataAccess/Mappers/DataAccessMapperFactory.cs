using AutoMapper;
using CashFlow.Core.DTO;
using CashFlow.DataAccess.Entities;
using Microsoft.Extensions.Logging.Abstractions;

namespace CashFlow.DataAccess.Mappers
{
    public class DataAccessMapperFactory : IDisposable
    {
        private IMapper mapper;
        private bool disposedValue;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    mapper = null!;
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        ~DataAccessMapperFactory() { Dispose(disposing: false); }

        public DataAccessMapperFactory()
        {
            var config = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<AccountEntity, AccountDTO>().ReverseMap();
                cfg.CreateMap<AccountPayableEntity, AccountPayableDTO>().ReverseMap();
                cfg.CreateMap<AccountReceivableEntity, AccountReceivableDTO>().ReverseMap();
                cfg.CreateMap<ExpenseTypeEntity, ExpenseTypeDTO>().ReverseMap();
                cfg.CreateMap<FixedExpenseEntity, FixedExpenseDTO>().ReverseMap();
                cfg.CreateMap<RevenueTypeEntity, RevenueTypeDTO>().ReverseMap();
                cfg.CreateMap<UserEntity, UserDTO>().ReverseMap();
            }, NullLoggerFactory.Instance);

            this.mapper = config.CreateMapper();
        }

        public IMapper getMapper()
        {
            return mapper;
        }
    }
}
