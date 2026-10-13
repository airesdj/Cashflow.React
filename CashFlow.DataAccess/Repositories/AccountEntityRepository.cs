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

        public DateTime? GetPeriodStartFromMainSalary()
        {
            try
            {
                var salaray = _context.accountsreceivable.AsNoTracking()
                                                   .Where(x => x.AccountOID == MAIN_ACCOUNT_OID &&
                                                               x.RevenueTypeOID == MAIN_REVENUE_SALARY_OID)
                                                   .OrderByDescending(x => x.OID)
                                                   .FirstOrDefault();
                if (salaray == null)
                    return DateTime.Now;
                else
                    return salaray.DueDate;
            }
            catch (Exception ex)
            {
                Log.Error(ex);
                return null!;
            }
        }

        public List<AccountBalanceModel> GetAllAccountBalance(int mes, int year)
        {
            try
            {
                List<AccountBalanceModel> list = new List<AccountBalanceModel>();

                DateTime dtfrom = new DateTime(year, mes, 1);
                DateTime dtto = dtfrom.AddMonths(1).AddDays(-1);

                list = (from a in _context.accounts.AsNoTracking()
                        orderby a.OID
                        select new AccountBalanceModel()
                        {
                            OID = a.OID,
                            Name = a.Name,
                            CurrencyType = a.CurrencyType!,
                            Balance = 0
                        }).ToList();

                list.ForEach(item =>
                {
                    item.Balance = GetAccountPreviousBalance(item.OID, dtto.AddDays(1), true);
                    item.TotalRevenue = GetAccountTotalRevenueForPeriod(item.OID, dtfrom, dtto);
                    item.TotalRevenueAires = GetTotalAccountReneueAiresPayment(item.OID, dtfrom, dtto);
                    item.TotalRevenueMarys = GetTotalAccountReneueMarysPayment(item.OID, dtfrom, dtto);
                });

                return list;
            }
            catch (Exception ex)
            {
                Log.Error(ex);
                return null!;
            }
        }

        private decimal GetAccountBalanceToday(long oid, DateTime from, DateTime to)
        {
            decimal balance = 0;

            StringBuilder strQuery = new StringBuilder();

            strQuery.Append(" SELECT IFNULL(SUM(AccountValue), 0) as total");
            strQuery.Append(" FROM account_payable");
            strQuery.Append($" WHERE DueDate >= '{from.ToString("yyyy-MM-dd")}' AND DueDate <= '{to.ToString("yyyy-MM-dd")}'");
            strQuery.Append($" AND AccountOID = {oid} ");
            decimal total_payable = 0;
            try { total_payable = _context.Database.SqlQueryRaw<QueryResultModel>(strQuery.ToString()).Select(q => q.Total).FirstOrDefault(); } catch { total_payable = 0; }

            strQuery.Clear();

            strQuery.Append(" SELECT IFNULL(SUM(AccountValue), 0) as total");
            strQuery.Append(" FROM account_receivable");
            strQuery.Append($" WHERE DueDate >= '{from.ToString("yyyy-MM-dd")}' AND DueDate <= '{to.ToString("yyyy-MM-dd")}'");
            strQuery.Append($" AND AccountOID = {oid} ");
            decimal total_receivable = 0;
            try { total_receivable = _context.Database.SqlQueryRaw<QueryResultModel>(strQuery.ToString()).Select(q => q.Total).FirstOrDefault(); } catch { total_receivable = 0; }

            balance = total_receivable - total_payable;

            strQuery.Clear();
            strQuery = null!;

            return balance;
        }

        public AccountDTO GetAccount(long oid)
        {
            try
            {
                return mapperFactory.getMapper().Map<AccountDTO>(_context.accounts.AsNoTracking().FirstOrDefault(x => x.OID == oid));
            }
            catch (Exception ex)
            {
                Log.Error(ex);
                return null!;
            }
        }

        public bool GetAccountByName(string name, long oid = 0)
            => _context.accounts.AsNoTracking().Any(x => x.Name == name && (oid == 0 || x.OID != oid));

        public List<AccountDTO> GetAccountList()
        {
            try
            {
                List<AccountDTO> list = new List<AccountDTO>();

                foreach (var item in _context.accounts.AsNoTracking().OrderBy(o => o.OID))
                    list.Add(mapperFactory.getMapper().Map<AccountDTO>(item));

                return list;
            }
            catch (Exception ex)
            {
                Log.Error(ex);
                return null!;
            }
        }

        public long Add(AccountDTO model)
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
            AccountEntity? account = null;

            try
            {
                account = _context.accounts.FirstOrDefault(a => a.OID == oid);

                if (account != null)
                {
                    _context.accounts.Attach(account);
                    _context.accounts.Remove(account);
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

        public bool Update(AccountDTO model)
        {
            try
            {
                bool ret = false;

                var account = _context.accounts.FirstOrDefault(x => x.OID == model.OID);
                if (account != null)
                {
                    account.Name = model.Name;
                    account.CurrencyType = model.CurrencyType;

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

        public decimal GetAccountPreviousBalance(long oid, DateTime launch_date, bool isCurrentBalance = false)
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

            try { total_payable = _context.Database.SqlQueryRaw<QueryResultModel>(strQuery.ToString(), launch_date.ToString("yyyy-MM-dd"), oid).Select(q => q.Total).FirstOrDefault(); } catch { total_payable = 0; }

            strQuery.Clear();

            strQuery.Append(" SELECT IFNULL(SUM(AccountValue), 0) as total");
            strQuery.Append(" FROM account_receivable");
            strQuery.Append(" WHERE AccountOID = @p0 ");
            strQuery.Append(" AND DueDate < @p1");
            decimal total_receivable = 0;
            try { total_receivable = _context.Database.SqlQueryRaw<QueryResultModel>(strQuery.ToString(), oid, launch_date.ToString("yyyy-MM-dd")).Select(q => q.Total).FirstOrDefault(); } catch { total_receivable = 0; }

            balance = total_receivable - total_payable;

            strQuery.Clear();
            strQuery = null!;

            return balance;
        }

        public decimal GetTotalAccountReneueAiresPayment(long oid, DateTime from, DateTime to)
        {
            decimal total_receivable = 0;

            StringBuilder strQuery = new StringBuilder();

            strQuery.Append(" SELECT IFNULL(SUM(AccountValue), 0) as total");
            strQuery.Append(" FROM account_receivable");
            strQuery.Append($" WHERE AccountOID = {oid} ");
            strQuery.Append($" AND DueDate >= '{from.ToString("yyyy-MM-dd")}' AND DueDate <= '{to.ToString("yyyy-MM-dd")}'");
            strQuery.Append(" AND RevenueTypeOID = 12 ");
            try { total_receivable = _context.Database.SqlQueryRaw<QueryResultModel>(strQuery.ToString()).Select(q => q.Total).FirstOrDefault(); } catch { total_receivable = 0; }

            strQuery.Clear();
            strQuery = null!;

            return total_receivable;
        }

        public decimal GetTotalAccountReneueMarysPayment(long oid, DateTime from, DateTime to)
        {
            decimal total_receivable = 0;

            StringBuilder strQuery = new StringBuilder();

            strQuery.Append(" SELECT IFNULL(SUM(AccountValue), 0) as total");
            strQuery.Append(" FROM account_receivable");
            strQuery.Append($" WHERE AccountOID = {oid} ");
            strQuery.Append($" AND DueDate >= '{from.ToString("yyyy-MM-dd")}' AND DueDate <= '{to.ToString("yyyy-MM-dd")}'");
            strQuery.Append(" AND RevenueTypeOID = 1 ");
            try { total_receivable = _context.Database.SqlQueryRaw<QueryResultModel>(strQuery.ToString()).Select(q => q.Total).FirstOrDefault(); } catch { total_receivable = 0; }

            strQuery.Clear();
            strQuery = null!;

            return total_receivable;
        }

        public decimal GetAccountFinalBalance(long oid, DateTime launch_date)
        {
            decimal balance = 0;

            StringBuilder strQuery = new StringBuilder();

            strQuery.Append(" SELECT IFNULL(SUM(AccountValue), 0) as total");
            strQuery.Append(" FROM account_payable");
            strQuery.Append($" WHERE DueDate <= '{launch_date.ToString("yyyy-MM-dd")}'");
            strQuery.Append($" AND AccountOID = {oid} ");
            decimal total_payable = 0;
            try { total_payable = _context.Database.SqlQueryRaw<QueryResultModel>(strQuery.ToString()).Select(q => q.Total).FirstOrDefault(); } catch { total_payable = 0; }

            strQuery.Clear();

            strQuery.Append(" SELECT IFNULL(SUM(AccountValue), 0) as total");
            strQuery.Append(" FROM account_receivable");
            strQuery.Append($" WHERE DueDate <= '{launch_date.ToString("yyyy-MM-dd")}'");
            strQuery.Append($" AND AccountOID = {oid} ");
            decimal total_receivable = 0;
            try { total_receivable = _context.Database.SqlQueryRaw<QueryResultModel>(strQuery.ToString()).Select(q => q.Total).FirstOrDefault(); } catch { total_receivable = 0; }

            balance = total_receivable - total_payable;

            strQuery.Clear();
            strQuery = null!;

            return balance;
        }

        public Tuple<decimal, decimal> GetAccountTotalForPeriod(long oid, long expensetypetoexcludeoid, long expensetypeoid, long revenuetypeoid, DateTime launch_start, DateTime launch_end)
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
            try { total_payable = _context.Database.SqlQueryRaw<QueryResultModel>(strQuery.ToString()).Select(q => q.Total).FirstOrDefault(); } catch { total_payable = 0; }

            strQuery.Clear();

            strQuery.Append(" SELECT IFNULL(SUM(AccountValue), 0) as total");
            strQuery.Append(" FROM account_receivable");
            strQuery.Append($" WHERE DueDate >= '{launch_start.ToString("yyyy-MM-dd")}' AND DueDate <= '{launch_end.ToString("yyyy-MM-dd")}'");
            strQuery.Append($" AND AccountOID = {oid} ");
            if (expensetypetoexcludeoid == 12)
                strQuery.Append($" AND RevenueTypeOID != {expensetypetoexcludeoid + 1} ");
            if (revenuetypeoid > 0)
                strQuery.Append($" AND RevenueTypeOID = {revenuetypeoid} ");
            try { total_receivable = _context.Database.SqlQueryRaw<QueryResultModel>(strQuery.ToString()).Select(q => q.Total).FirstOrDefault(); } catch { total_receivable = 0; }

            strQuery.Clear();
            strQuery = null!;

            return new Tuple<decimal, decimal>(total_payable, total_receivable);
        }

        public decimal GetAccountTotalRevenueForPeriod(long oid, DateTime launch_start, DateTime launch_end)
        {
            StringBuilder strQuery = new StringBuilder();
            decimal total_receivable = 0;

            strQuery.Append(" SELECT IFNULL(SUM(AccountValue), 0) as total");
            strQuery.Append(" FROM account_receivable");
            strQuery.Append($" WHERE DueDate >= '{launch_start.ToString("yyyy-MM-dd")}' AND DueDate <= '{launch_end.ToString("yyyy-MM-dd")}'");
            strQuery.Append($" AND AccountOID = {oid} ");
            strQuery.Append($" AND RevenueTypeOID NOT IN (13,18) ");
            try { total_receivable = _context.Database.SqlQueryRaw<QueryResultModel>(strQuery.ToString()).Select(q => q.Total).FirstOrDefault(); } catch { total_receivable = 0; }

            strQuery.Clear();
            strQuery = null!;

            return total_receivable;
        }

        public decimal GetAccountTotalTransferToSaver(int mes, int year)
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
            try { total_payable = _context.Database.SqlQueryRaw<QueryResultModel>(strQuery.ToString()).Select(q => q.Total).FirstOrDefault(); } catch { total_payable = 0; }

            strQuery.Clear();

            strQuery.Append(" SELECT IFNULL(SUM(AccountValue), 0) as total");
            strQuery.Append(" FROM account_receivable");
            strQuery.Append($" WHERE DueDate >= '{from.ToString("yyyy-MM-dd")}' AND DueDate <= '{to.ToString("yyyy-MM-dd")}'");
            strQuery.Append($" AND AccountOID = 1 ");
            strQuery.Append($" AND RevenueTypeOID = 15 ");
            try { total_receivable = _context.Database.SqlQueryRaw<QueryResultModel>(strQuery.ToString()).Select(q => q.Total).FirstOrDefault(); } catch { total_receivable = 0; }

            total_payable = total_payable - total_receivable;

            strQuery.Clear();
            strQuery = null!;

            return total_payable;
        }

        public decimal GetAccountTotalTransferMaryPayment(int mes, int year)
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
            try { total_account = _context.Database.SqlQueryRaw<QueryResultModel>(sql).Select(q => q.Total).FirstOrDefault(); } catch { total_account = 0; }

            strQuery.Clear();

            strQuery.Append(" SELECT IFNULL(SUM(AccountValue), 0) as total");
            strQuery.Append(" FROM account_receivable");
            strQuery.Append($" WHERE DueDate >= '{from.ToString("yyyy-MM-dd")}' AND DueDate <= '{to.ToString("yyyy-MM-dd")}'");
            strQuery.Append($" AND AccountOID = 7 ");
            strQuery.Append($" AND RevenueTypeOID = 1 ");
            sql = strQuery.ToString();
            try { total_saver = _context.Database.SqlQueryRaw<QueryResultModel>(sql).Select(q => q.Total).FirstOrDefault(); } catch { total_saver = 0; }

            total_saver = total_saver - total_account;

            strQuery.Clear();
            strQuery = null!;

            return total_saver;
        }
    }
}
