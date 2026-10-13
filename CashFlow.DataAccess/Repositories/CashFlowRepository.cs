using CashFlow.Core.Models;
using CashFlow.DataAccess.Repositories.Base;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using static CashFlow.Core.Models.CashFlowModel;

namespace CashFlow.DataAccess.Repositories
{
    public class CashFlowRepository : BaseRepository
    {
        private readonly AccountEntityRepository _accountRepo;
        private readonly Context _context;
        public CashFlowRepository(Context context, AccountEntityRepository accountRepo, ILogger<BaseRepository> logger) : base(logger)
        {
            _accountRepo = accountRepo;
            _context = context;
        }

        protected override void Dispose(bool disposing)
            => base.Dispose(disposing);


        public Tuple<List<AccountModel>, CashFlowModel> Get(long acccoundoid, long expensetypetoexcludeoid, DateTime launchstart, DateTime launchend)
        {
            try
            {
                CashFlowModel model = new CashFlowModel();
                model.PreviousBalance = _accountRepo.GetAccountPreviousBalance(acccoundoid, launchstart);
                model.FinalBalance = _accountRepo.GetAccountFinalBalance(acccoundoid, launchend);

                var total = _accountRepo.GetAccountTotalForPeriod(acccoundoid, expensetypetoexcludeoid, 0, 0, launchstart, launchend);

                model.TotalExpenses = total.Item1;
                model.TotalRevenues = total.Item2;

                List<AccountModel> list = new List<AccountModel>();

                string currencytype = _context.accounts.AsNoTracking().Where(x => x.OID == acccoundoid).Select(x => x.CurrencyType).FirstOrDefault()!;
                string desc = string.Empty;

                var listRev = _context.revenues.AsNoTracking().OrderBy(x => x.OID).ToList();
                var listExp = _context.expenses.AsNoTracking().OrderBy(x => x.OID).ToList();

                _context.accountsreceivable.AsNoTracking()
                                      .Where(x => x.AccountOID == acccoundoid && x.DueDate >= launchstart && x.DueDate <= launchend)
                                      .ToList()
                                      .ForEach(account =>
                                      {
                                          desc = listRev.FirstOrDefault(x => x.OID == account.RevenueTypeOID)?.Name!;
                                          list.Add(new AccountModel() { AccountOID = acccoundoid, AccountValue = account.AccountValue ?? 0, DueDate = account.DueDate, Payday = account.Payday, CurrencyType = currencytype, Description = $"{desc} - {account.Description}", LaunchType = "REVENUE" });
                                      });

                if (expensetypetoexcludeoid == 0)
                {
                    _context.accountspayable.AsNoTracking()
                                       .Where(x => x.AccountOID == acccoundoid && x.DueDate >= launchstart && x.DueDate <= launchend)
                                       .ToList()
                                       .ForEach(account =>
                                       {
                                           desc = listExp.FirstOrDefault(x => x.OID == account.ExpenseTypeOID)?.Name!;
                                           list.Add(new AccountModel() { AccountOID = acccoundoid, AccountValue = account.AccountValue, DueDate = account.DueDate, Payday = account.Payday, CurrencyType = currencytype, Description = $"{desc} - {account.Description}", LaunchType = "EXPENSE" });
                                       });
                }
                else
                {
                    _context.accountspayable.AsNoTracking()
                                       .Where(x => x.AccountOID == acccoundoid && x.ExpenseTypeOID != expensetypetoexcludeoid && x.DueDate >= launchstart && x.DueDate <= launchend)
                                       .ToList()
                                       .ForEach(account =>
                                       {
                                           desc = listExp.FirstOrDefault(x => x.OID == account.ExpenseTypeOID)?.Name!;
                                           list.Add(new AccountModel() { AccountOID = acccoundoid, AccountValue = account.AccountValue, DueDate = account.DueDate, Payday = account.Payday, CurrencyType = currencytype, Description = $"{desc} - {account.Description}", LaunchType = "EXPENSE" });
                                       });
                }

                return new Tuple<List<AccountModel>, CashFlowModel>(list.OrderBy(x => x.DueDate).ThenBy(x => x.Payday).ToList(), model);
            }
            catch (Exception ex)
            {
                Log.Error(ex);
                return null!;
            }
        }
    }
}
