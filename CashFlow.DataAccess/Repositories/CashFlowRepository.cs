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


        public async Task<Tuple<List<AccountModel>, CashFlowModel>> Get(long acccoundoid, long expensetypetoexcludeoid,
                                                                        DateTime launchstart, DateTime launchend)
        {
            try
            {
                CashFlowModel model = new CashFlowModel();
                model.PreviousBalance = await _accountRepo.GetAccountPreviousBalance(acccoundoid, launchstart);
                model.FinalBalance = await _accountRepo.GetAccountFinalBalance(acccoundoid, launchend);

                var total = await _accountRepo.GetAccountTotalForPeriod(acccoundoid, expensetypetoexcludeoid, 0, 0, launchstart, launchend);

                model.TotalExpenses = total.Item1;
                model.TotalRevenues = total.Item2;

                List<AccountModel> list = new List<AccountModel>();

                string? currencytype = await _context.accounts.AsNoTracking().Where(x => x.OID == acccoundoid)
                                                .Select(x => x.CurrencyType).FirstOrDefaultAsync();
                string desc = string.Empty;

                var listRev = await _context.revenues.AsNoTracking().OrderBy(x => x.OID).ToArrayAsync();
                var listExp = await _context.expenses.AsNoTracking().OrderBy(x => x.OID).ToArrayAsync();


                var accountsR = await _context.accountsreceivable.AsNoTracking()
                                      .Where(x => x.AccountOID == acccoundoid && x.DueDate >= launchstart && x.DueDate <= launchend)
                                      .ToArrayAsync();

                list.AddRange(accountsR.Select(account => new AccountModel()
                {
                    AccountOID = acccoundoid,
                    AccountValue = account.AccountValue ?? 0,
                    DueDate = account.DueDate,
                    Payday = account.Payday,
                    CurrencyType = currencytype ?? "$",
                    Description = $"{listRev.FirstOrDefault(x => x.OID == account.RevenueTypeOID)?.Name} - {account.Description}",
                    LaunchType = "REVENUE"
                }).ToArray());

                if (expensetypetoexcludeoid == 0)
                {
                    var accountsP = await _context.accountspayable.AsNoTracking()
                                          .Where(x => x.AccountOID == acccoundoid && x.DueDate >= launchstart && x.DueDate <= launchend)
                                          .ToArrayAsync();

                    list.AddRange(accountsP.Select(account => new AccountModel()
                    {
                        AccountOID = acccoundoid,
                        AccountValue = account.AccountValue,
                        DueDate = account.DueDate,
                        Payday = account.Payday,
                        CurrencyType = currencytype ?? "$",
                        Description = $"{listExp.FirstOrDefault(x => x.OID == account.ExpenseTypeOID)?.Name} - {account.Description}",
                        LaunchType = "EXPENSE"
                    }).ToArray());
                }
                else
                {
                    var accountsP = await _context.accountspayable.AsNoTracking()
                                            .Where(x => x.AccountOID == acccoundoid && x.ExpenseTypeOID != expensetypetoexcludeoid && x.DueDate >= launchstart && x.DueDate <= launchend)
                                          .ToArrayAsync();

                    list.AddRange(accountsP.Select(account => new AccountModel()
                    {
                        AccountOID = acccoundoid,
                        AccountValue = account.AccountValue,
                        DueDate = account.DueDate,
                        Payday = account.Payday,
                        CurrencyType = currencytype ?? "$",
                        Description = $"{listExp.FirstOrDefault(x => x.OID == account.ExpenseTypeOID)?.Name} - {account.Description}",
                        LaunchType = "EXPENSE"
                    }).ToArray());
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
