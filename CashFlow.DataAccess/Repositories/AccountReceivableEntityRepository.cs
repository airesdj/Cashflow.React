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

        public async Task<AccountReceivableDTO> GetAccount(long oid)
        {
            try
            {
                var account = await _context.accountsreceivable.AsNoTracking().FirstOrDefaultAsync(x => x.OID == oid);
                return mapperFactory.getMapper().Map<AccountReceivableDTO>(account);
            }
            catch (Exception ex)
            {
                Log.Error(ex);
                return null!;
            }
        }

        public async Task<List<AccountReceivableDTO>> GetAccountList(long acccoundoid, long revenueoid, DateTime start, DateTime end)
        {
            try
            {
                AccountReceivableEntity[] accounts;

                if (revenueoid == 0)
                {
                    accounts = await _context.accountsreceivable.AsNoTracking()
                                           .Where(x => x.AccountOID == acccoundoid && x.DueDate >= start && x.DueDate <= end)
                                           .ToArrayAsync();
                }
                else
                {
                    accounts = await _context.accountsreceivable.AsNoTracking()
                                           .Where(x => x.AccountOID == acccoundoid && x.RevenueTypeOID == revenueoid && x.DueDate >= start && x.DueDate <= end)
                                           .ToArrayAsync();
                }

                var mapper = mapperFactory.getMapper();
                return accounts
                    .Select(item => mapper.Map<AccountReceivableDTO>(item))
                    .OrderBy(x => x.DueDate)
                    .ThenBy(x => x.Payday)
                    .ToList();
            }
            catch (Exception ex)
            {
                Log.Error(ex);
                return null!;
            }
        }

        public async Task<long> Add(AccountReceivableDTO model)
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
                await _context.SaveChangesAsync();
                id = item.OID;

                return id;
            }
            catch (Exception ex)
            {
                Log.Error(ex);
                return 0;
            }
        }

        public async Task<bool> Delete(long oid)
        {
            try
            {
                var deleted = await _context.accountsreceivable
                                    .Where(a => a.OID == oid)
                                    .ExecuteDeleteAsync();

                return deleted > 0 ? true : false;
            }
            catch (Exception ex)
            {
                Log.Error(ex);
                return false;
            }
        }

        public async Task<bool> Update(AccountReceivableDTO model)
        {
            try
            {
                bool ret = false;

                var account = await _context.accountsreceivable.FirstOrDefaultAsync(x => x.OID == model.OID);
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

                    await _context.SaveChangesAsync();

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
