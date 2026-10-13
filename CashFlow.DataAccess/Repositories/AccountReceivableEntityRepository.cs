using CashFlow.Core.DTO;
using CashFlow.DataAccess.Entities;
using CashFlow.DataAccess.Repositories.Base;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CashFlow.DataAccess.Repositories
{
    public class AccountReceivableEntityRepository : BaseRepository
    {
        private readonly Context _context;
        public AccountReceivableEntityRepository(Context context, ILogger<BaseRepository> logger) : base(logger)
        {
            _context = context;
        }

        protected override void Dispose(bool disposing)
            => base.Dispose(disposing);

        public AccountReceivableDTO GetAccount(long oid)
        {
            try
            {
                return mapperFactory.getMapper().Map<AccountReceivableDTO>(_context.accountsreceivable.AsNoTracking().FirstOrDefault(x => x.OID == oid));
            }
            catch (Exception ex)
            {
                Log.Error(ex);
                return null!;
            }
        }

        public List<AccountReceivableDTO> GetAccountList(long acccoundoid, long revenueoid, DateTime start, DateTime end)
        {
            try
            {
                List<AccountReceivableDTO> list = new List<AccountReceivableDTO>();

                if (revenueoid == 0)
                {
                    _context.accountsreceivable.AsNoTracking()
                                           .Where(x => x.AccountOID == acccoundoid && x.DueDate >= start && x.DueDate <= end)
                                          .ToList()
                                          .ForEach(account => list.Add(mapperFactory.getMapper().Map<AccountReceivableDTO>(account)));
                }
                else
                {
                    _context.accountsreceivable.AsNoTracking()
                                          .Where(x => x.AccountOID == acccoundoid && x.RevenueTypeOID == revenueoid && x.DueDate >= start && x.DueDate <= end)
                                          .ToList()
                                          .ForEach(account => list.Add(mapperFactory.getMapper().Map<AccountReceivableDTO>(account)));
                }

                return list.OrderBy(x => x.DueDate).ThenBy(x => x.Payday).ToList();
            }
            catch (Exception ex)
            {
                Log.Error(ex);
                return null!;
            }
        }

        public long Add(AccountReceivableDTO model)
        {
            try
            {
                long id = 0;
                AccountReceivableEntity item = new AccountReceivableEntity
                {
                    AccountOID = model.AccountOID,
                    DueDate = model.DueDate,
                    Description = model.Description,
                    RevenueTypeOID = model.RevenueTypeOID,
                    Payday = model.Payday,
                    AccountValue = model.AccountValue,
                    CreatedBy = model.CreatedBy,
                    CreatedDate = model.CreatedDate
                };

                _context.accountsreceivable.Add(item);
                _context.SaveChanges();
                id = item.OID;

                return id;
            }
            catch (Exception ex)
            {
                Log.Error(ex);
                return 0;
            }
        }

        public bool Delete(long oid)
        {
            AccountReceivableEntity? account = null;

            try
            {
                account = _context.accountsreceivable.FirstOrDefault(x => x.OID == oid);
                if (account != null)
                {
                    _context.accountsreceivable.Attach(account);
                    _context.accountsreceivable.Remove(account);
                    _context.SaveChanges();
                }

                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex);
                return false;
            }
            finally
            {
                account = null;
            }
        }

        public bool Update(AccountReceivableDTO model)
        {
            try
            {
                bool ret = false;

                var account = _context.accountsreceivable.FirstOrDefault(x => x.OID == model.OID);
                if (account != null)
                {
                    account.AccountOID = model.AccountOID;
                    account.DueDate = model.DueDate;
                    account.Description = model.Description;
                    account.RevenueTypeOID = model.RevenueTypeOID;
                    account.Payday = model.Payday;
                    account.AccountValue = model.AccountValue;
                    account.UpdatedBy = model.UpdatedBy;
                    account.UpdatedDate = model.UpdatedDate;

                    _context.Entry(account).State = EntityState.Modified;
                    _context.SaveChanges();

                    ret = true;
                }
                else
                    Log.Error("Account NOT found.");

                return ret;
            }
            catch (Exception ex)
            {
                Log.Error(ex);
                return false;
            }
        }
    }
}
