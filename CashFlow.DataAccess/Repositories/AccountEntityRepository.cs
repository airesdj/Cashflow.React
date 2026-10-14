using CashFlow.Core.DTO;
using CashFlow.Core.Models;
using CashFlow.DataAccess.Entities;
using CashFlow.DataAccess.Repositories.Base;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text;

namespace CashFlow.DataAccess.Repositories
{
    public class AccountEntityRepository : BaseRepository
    {
        public long MAIN_REVENUE_SALARY_OID = 12;
        public long MAIN_ACCOUNT_OID = 1;
        private readonly Context _context;
        public AccountEntityRepository(Context context, ILogger<BaseRepository> logger) : base(logger)
        {
            _context = context;
        }

        protected override void Dispose(bool disposing)
            => base.Dispose(disposing);

        public async Task<DateTime?> GetPeriodStartFromMainSalary()
        {
            try
            {
                DateTime? duedate = await _context.accountsreceivable.AsNoTracking()
                                                   .Where(x => x.AccountOID == MAIN_ACCOUNT_OID &&
                                                               x.RevenueTypeOID == MAIN_REVENUE_SALARY_OID)
                                                   .OrderByDescending(x => x.OID)
                                                   .Select(x => x.DueDate)
                                                   .FirstOrDefaultAsync();
                return duedate ?? DateTime.Now;
            }
            catch (Exception ex)
            {
                Log.Error(ex);
                return null;
            }
        }

        public async Task<List<AccountBalanceModel>> GetAllAccountBalance(int mes, int year)
        {
            try
            {
                DateTime dtfrom = new DateTime(year, mes, 1);
                DateTime dtto = dtfrom.AddMonths(1).AddDays(-1);

                var list = await _context.accounts.AsNoTracking()
                                .OrderBy(x => x.OID)
                                .Select(a => new AccountBalanceModel()
                                {
                                    OID = a.OID,
                                    Name = a.Name,
                                    CurrencyType = a.CurrencyType!,
                                    Balance = 0
                                }).ToListAsync();

                foreach (var item in list)
                {
                    item.Balance = await GetAccountPreviousBalance(item.OID, dtto.AddDays(1), true);
                    item.TotalRevenue = await GetAccountTotalRevenueForPeriod(item.OID, dtfrom, dtto);
                    item.TotalRevenueAires = await GetTotalAccountReneueAiresPayment(item.OID, dtfrom, dtto);
                    item.TotalRevenueMarys = await GetTotalAccountReneueMarysPayment(item.OID, dtfrom, dtto);
                }

                return list;
            }
            catch (Exception ex)
            {
                Log.Error(ex);
                return null!;
            }
        }

        private async Task<decimal> GetAccountBalanceToday(long oid, DateTime from, DateTime to)
        {
            decimal balance = 0;

            StringBuilder strQuery = new StringBuilder();

            strQuery.Append(" SELECT IFNULL(SUM(AccountValue), 0) as total");
            strQuery.Append(" FROM account_payable");
            strQuery.Append($" WHERE DueDate >= '{from.ToString("yyyy-MM-dd")}' AND DueDate <= '{to.ToString("yyyy-MM-dd")}'");
            strQuery.Append($" AND AccountOID = {oid} ");
            decimal total_payable = 0;
            try
            {
                total_payable = await _context.Database.SqlQueryRaw<QueryResultModel>(strQuery.ToString())
                                    .Select(q => q.Total).FirstOrDefaultAsync();
            }
            catch { total_payable = 0; }

            strQuery.Clear();

            strQuery.Append(" SELECT IFNULL(SUM(AccountValue), 0) as total");
            strQuery.Append(" FROM account_receivable");
            strQuery.Append($" WHERE DueDate >= '{from.ToString("yyyy-MM-dd")}' AND DueDate <= '{to.ToString("yyyy-MM-dd")}'");
            strQuery.Append($" AND AccountOID = {oid} ");
            decimal total_receivable = 0;
            try
            {
                total_receivable = await _context.Database
                                .SqlQueryRaw<QueryResultModel>(strQuery.ToString()).Select(q => q.Total)
                                .FirstOrDefaultAsync();
            }
            catch { total_receivable = 0; }

            balance = total_receivable - total_payable;

            strQuery.Clear();
            strQuery = null!;

            return balance;
        }

        public async Task<AccountDTO> GetAccount(long oid)
        {
            try
            {
                var account = await _context.accounts.AsNoTracking().FirstOrDefaultAsync(x => x.OID == oid);
                return mapperFactory.getMapper().Map<AccountDTO>(account);
            }
            catch (Exception ex)
            {
                Log.Error(ex);
                return null!;
            }
        }

        public Task<bool> GetAccountByName(string name, long oid = 0)
            => _context.accounts.AsNoTracking().AnyAsync(x => x.Name == name && (oid == 0 || x.OID != oid));

        public async Task<List<AccountDTO>> GetAccountList()
        {
            try
            {
                var accounts = await _context.accounts.AsNoTracking()
                                .OrderBy(o => o.OID)
                                .ToArrayAsync();

                var mapper = mapperFactory.getMapper();

                return accounts
                            .Select(item => mapper.Map<AccountDTO>(item))
                            .ToList();
            }
            catch (Exception ex)
            {
                Log.Error(ex);
                return null!;
            }
        }

        public async Task<long> Add(AccountDTO model)
        {
            try
            {
                long id = 0;
                AccountEntity item = new AccountEntity
                {
                    Name = model.Name,
                    CurrencyType = model.CurrencyType
                };

                _context.accounts.Add(item);
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
                var deleted = await _context.accounts
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

        public async Task<bool> Update(AccountDTO model)
        {
            try
            {
                var updated = await _context.accounts
                                .Where(x => x.OID == model.OID)
                                .ExecuteUpdateAsync(updates => updates
                                    .SetProperty(h => h.Name, _ => model.Name)
                                    .SetProperty(h => h.CurrencyType, _ => model.CurrencyType));

                if (updated == 0)
                    Log.Error("Account NOT found.");

                return updated > 0 ? true : false;
            }
            catch (Exception ex)
            {
                Log.Error(ex);
                return false;
            }
        }

        public async Task<decimal> GetAccountPreviousBalance(long oid, DateTime launch_date, bool isCurrentBalance = false)
        {
            decimal balance = 0;

            StringBuilder strQuery = new StringBuilder();

            strQuery.Append(" SELECT IFNULL(SUM(AccountValue), 0) as total");
            strQuery.Append(" FROM account_payable");
            strQuery.Append(" WHERE DueDate < @p0");
            if (isCurrentBalance)
                strQuery.Append(" AND PayDay IS NOT NULL");
            strQuery.Append(" AND AccountOID = @p1 ");
            decimal total_payable = 0;

            try
            {
                total_payable = await _context.Database.SqlQueryRaw<QueryResultModel>(strQuery.ToString(), launch_date.ToString("yyyy-MM-dd"), oid)
                                    .Select(q => q.Total).FirstOrDefaultAsync();
            }
            catch { total_payable = 0; }

            strQuery.Clear();

            strQuery.Append(" SELECT IFNULL(SUM(AccountValue), 0) as total");
            strQuery.Append(" FROM account_receivable");
            strQuery.Append(" WHERE AccountOID = @p0 ");
            strQuery.Append(" AND DueDate < @p1");
            decimal total_receivable = 0;
            try
            {
                total_receivable = await _context.Database.SqlQueryRaw<QueryResultModel>(strQuery.ToString(), oid, launch_date.ToString("yyyy-MM-dd"))
                                        .Select(q => q.Total).FirstOrDefaultAsync();
            }
            catch { total_receivable = 0; }

            balance = total_receivable - total_payable;

            strQuery.Clear();
            strQuery = null!;

            return balance;
        }

        public async Task<decimal> GetTotalAccountReneueAiresPayment(long oid, DateTime from, DateTime to)
        {
            decimal total_receivable = 0;

            StringBuilder strQuery = new StringBuilder();

            strQuery.Append(" SELECT IFNULL(SUM(AccountValue), 0) as total");
            strQuery.Append(" FROM account_receivable");
            strQuery.Append($" WHERE AccountOID = {oid} ");
            strQuery.Append($" AND DueDate >= '{from.ToString("yyyy-MM-dd")}' AND DueDate <= '{to.ToString("yyyy-MM-dd")}'");
            strQuery.Append(" AND RevenueTypeOID = 12 ");
            try
            {
                total_receivable = await _context.Database.SqlQueryRaw<QueryResultModel>(strQuery.ToString())
                            .Select(q => q.Total).FirstOrDefaultAsync();
            }
            catch { total_receivable = 0; }

            strQuery.Clear();
            strQuery = null!;

            return total_receivable;
        }

        public async Task<decimal> GetTotalAccountReneueMarysPayment(long oid, DateTime from, DateTime to)
        {
            decimal total_receivable = 0;

            StringBuilder strQuery = new StringBuilder();

            strQuery.Append(" SELECT IFNULL(SUM(AccountValue), 0) as total");
            strQuery.Append(" FROM account_receivable");
            strQuery.Append($" WHERE AccountOID = {oid} ");
            strQuery.Append($" AND DueDate >= '{from.ToString("yyyy-MM-dd")}' AND DueDate <= '{to.ToString("yyyy-MM-dd")}'");
            strQuery.Append(" AND RevenueTypeOID = 1 ");
            try
            {
                total_receivable = await _context.Database.SqlQueryRaw<QueryResultModel>(strQuery.ToString())
                                    .Select(q => q.Total).FirstOrDefaultAsync();
            }
            catch { total_receivable = 0; }

            strQuery.Clear();
            strQuery = null!;

            return total_receivable;
        }

        public async Task<decimal> GetAccountFinalBalance(long oid, DateTime launch_date)
        {
            decimal balance = 0;

            StringBuilder strQuery = new StringBuilder();

            strQuery.Append(" SELECT IFNULL(SUM(AccountValue), 0) as total");
            strQuery.Append(" FROM account_payable");
            strQuery.Append($" WHERE DueDate <= '{launch_date.ToString("yyyy-MM-dd")}'");
            strQuery.Append($" AND AccountOID = {oid} ");
            decimal total_payable = 0;
            try
            {
                total_payable = await _context.Database.SqlQueryRaw<QueryResultModel>(strQuery.ToString())
                                    .Select(q => q.Total).FirstOrDefaultAsync();
            }
            catch { total_payable = 0; }

            strQuery.Clear();

            strQuery.Append(" SELECT IFNULL(SUM(AccountValue), 0) as total");
            strQuery.Append(" FROM account_receivable");
            strQuery.Append($" WHERE DueDate <= '{launch_date.ToString("yyyy-MM-dd")}'");
            strQuery.Append($" AND AccountOID = {oid} ");
            decimal total_receivable = 0;
            try
            {
                total_receivable = await _context.Database.SqlQueryRaw<QueryResultModel>(strQuery.ToString())
                                        .Select(q => q.Total).FirstOrDefaultAsync();
            }
            catch { total_receivable = 0; }

            balance = total_receivable - total_payable;

            strQuery.Clear();
            strQuery = null!;

            return balance;
        }

        public async Task<Tuple<decimal, decimal>> GetAccountTotalForPeriod(long oid, long expensetypetoexcludeoid,
                                                                            long expensetypeoid, long revenuetypeoid,
                                                                            DateTime launch_start, DateTime launch_end)
        {
            StringBuilder strQuery = new StringBuilder();
            decimal total_payable = 0;
            decimal total_receivable = 0;

            strQuery.Append(" SELECT IFNULL(SUM(AccountValue), 0) as total");
            strQuery.Append(" FROM account_payable");
            strQuery.Append($" WHERE DueDate >= '{launch_start.ToString("yyyy-MM-dd")}' AND DueDate <= '{launch_end.ToString("yyyy-MM-dd")}'");
            strQuery.Append($" AND AccountOID = {oid} ");
            if (expensetypetoexcludeoid > 0)
                strQuery.Append($" AND ExpenseTypeOID != {expensetypetoexcludeoid} ");
            if (expensetypeoid > 0)
                strQuery.Append($" AND ExpenseTypeOID = {expensetypeoid} ");
            try
            {
                total_payable = await _context.Database.SqlQueryRaw<QueryResultModel>(strQuery.ToString())
                                        .Select(q => q.Total).FirstOrDefaultAsync();
            }
            catch { total_payable = 0; }

            strQuery.Clear();

            strQuery.Append(" SELECT IFNULL(SUM(AccountValue), 0) as total");
            strQuery.Append(" FROM account_receivable");
            strQuery.Append($" WHERE DueDate >= '{launch_start.ToString("yyyy-MM-dd")}' AND DueDate <= '{launch_end.ToString("yyyy-MM-dd")}'");
            strQuery.Append($" AND AccountOID = {oid} ");
            if (expensetypetoexcludeoid == 12)
                strQuery.Append($" AND RevenueTypeOID != {expensetypetoexcludeoid + 1} ");
            if (revenuetypeoid > 0)
                strQuery.Append($" AND RevenueTypeOID = {revenuetypeoid} ");
            try
            {
                total_receivable = await _context.Database.SqlQueryRaw<QueryResultModel>(strQuery.ToString())
                                        .Select(q => q.Total).FirstOrDefaultAsync();
            }
            catch { total_receivable = 0; }

            strQuery.Clear();
            strQuery = null!;

            return new Tuple<decimal, decimal>(total_payable, total_receivable);
        }

        public async Task<decimal> GetAccountTotalRevenueForPeriod(long oid, DateTime launch_start, DateTime launch_end)
        {
            StringBuilder strQuery = new StringBuilder();
            decimal total_receivable = 0;

            strQuery.Append(" SELECT IFNULL(SUM(AccountValue), 0) as total");
            strQuery.Append(" FROM account_receivable");
            strQuery.Append($" WHERE DueDate >= '{launch_start.ToString("yyyy-MM-dd")}' AND DueDate <= '{launch_end.ToString("yyyy-MM-dd")}'");
            strQuery.Append($" AND AccountOID = {oid} ");
            strQuery.Append($" AND RevenueTypeOID NOT IN (13,18) ");
            try
            {
                total_receivable = await _context.Database.SqlQueryRaw<QueryResultModel>(strQuery.ToString())
                                        .Select(q => q.Total).FirstOrDefaultAsync();
            }
            catch { total_receivable = 0; }

            strQuery.Clear();
            strQuery = null!;

            return total_receivable;
        }

        public async Task<decimal> GetAccountTotalTransferToSaver(int mes, int year)
        {
            StringBuilder strQuery = new StringBuilder();
            decimal total_payable = 0;
            decimal total_receivable = 0;
            DateTime from = new DateTime(year, mes, 1);
            DateTime to = from.AddMonths(1).AddDays(-1);

            strQuery.Append(" SELECT IFNULL(SUM(AccountValue), 0) as total");
            strQuery.Append(" FROM account_payable");
            strQuery.Append($" WHERE DueDate >= '{from.ToString("yyyy-MM-dd")}' AND DueDate <= '{to.ToString("yyyy-MM-dd")}'");
            strQuery.Append($" AND AccountOID = 1 ");
            strQuery.Append($" AND ExpenseTypeOID = 12 ");  //money transfer
            try
            {
                total_payable = await _context.Database.SqlQueryRaw<QueryResultModel>(strQuery.ToString())
                                    .Select(q => q.Total).FirstOrDefaultAsync();
            }
            catch { total_payable = 0; }

            strQuery.Clear();

            strQuery.Append(" SELECT IFNULL(SUM(AccountValue), 0) as total");
            strQuery.Append(" FROM account_receivable");
            strQuery.Append($" WHERE DueDate >= '{from.ToString("yyyy-MM-dd")}' AND DueDate <= '{to.ToString("yyyy-MM-dd")}'");
            strQuery.Append($" AND AccountOID = 1 ");
            strQuery.Append($" AND RevenueTypeOID = 15 ");
            try
            {
                total_receivable = await _context.Database.SqlQueryRaw<QueryResultModel>(strQuery.ToString())
                                        .Select(q => q.Total).FirstOrDefaultAsync();
            }
            catch { total_receivable = 0; }

            total_payable = total_payable - total_receivable;

            strQuery.Clear();
            strQuery = null!;

            return total_payable;
        }

        public async Task<decimal> GetAccountTotalTransferMaryPayment(int mes, int year)
        {
            StringBuilder strQuery = new StringBuilder();
            decimal total_account = 0;
            decimal total_saver = 0;
            DateTime from = new DateTime(year, mes, 1);
            DateTime to = from.AddMonths(1).AddDays(-1);
            var sql = string.Empty;

            strQuery.Append(" SELECT IFNULL(SUM(AccountValue), 0) as total");
            strQuery.Append(" FROM account_receivable");
            strQuery.Append($" WHERE DueDate >= '{from.ToString("yyyy-MM-dd")}' AND DueDate <= '{to.ToString("yyyy-MM-dd")}'");
            strQuery.Append($" AND AccountOID = 1 ");
            strQuery.Append($" AND RevenueTypeOID = 18 ");  //marys payment
            sql = strQuery.ToString();
            try
            {
                total_account = await _context.Database.SqlQueryRaw<QueryResultModel>(sql)
                                        .Select(q => q.Total).FirstOrDefaultAsync();
            }
            catch { total_account = 0; }

            strQuery.Clear();

            strQuery.Append(" SELECT IFNULL(SUM(AccountValue), 0) as total");
            strQuery.Append(" FROM account_receivable");
            strQuery.Append($" WHERE DueDate >= '{from.ToString("yyyy-MM-dd")}' AND DueDate <= '{to.ToString("yyyy-MM-dd")}'");
            strQuery.Append($" AND AccountOID = 7 ");
            strQuery.Append($" AND RevenueTypeOID = 1 ");
            sql = strQuery.ToString();
            try
            {
                total_saver = await _context.Database.SqlQueryRaw<QueryResultModel>(sql)
                                    .Select(q => q.Total).FirstOrDefaultAsync();
            }
            catch { total_saver = 0; }

            total_saver = total_saver - total_account;

            strQuery.Clear();
            strQuery = null!;

            return total_saver;
        }
    }
}
