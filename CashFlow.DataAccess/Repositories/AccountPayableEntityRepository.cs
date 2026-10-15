using CashFlow.Core.DTO;
using CashFlow.DataAccess.Entities;
using CashFlow.DataAccess.Repositories.Base;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using System.Data;

namespace CashFlow.DataAccess.Repositories
{
    public class AccountPayableEntityRepository : BaseRepository
    {
        private readonly Context _context;
        public AccountPayableEntityRepository(Context context, ILogger<BaseRepository> logger) : base(logger)
        {
            _context = context;
        }

        protected override void Dispose(bool disposing)
            => base.Dispose(disposing);

        public async Task<List<AccountPayableDTO>> GetAllAccountPayableByDay(DateTime day)
        {
            try
            {
                List<AccountPayableDTO> list = new List<AccountPayableDTO>();

                list = await (from a in _context.accountspayable.AsNoTracking()
                              join e in _context.expenses.AsNoTracking() on a.ExpenseTypeOID equals e.OID
                              where a.DueDate == day
                              select new AccountPayableDTO()
                              {
                                  OID = a.OID,
                                  AccountOID = a.AccountOID,
                                  DueDate = day,
                                  Description = e.Name,
                                  ExpenseTypeOID = a.ExpenseTypeOID,
                                  Payday = a.Payday,
                                  AccountValue = a.AccountValue,
                                  CreatedBy = a.CreatedBy,
                                  CreatedDate = a.CreatedDate,
                                  UpdatedBy = a.UpdatedBy,
                                  UpdatedDate = a.UpdatedDate
                              }).ToListAsync();

                return list;
            }
            catch (Exception ex)
            {
                Log.Error(ex);
                return null!;
            }
        }

        public async Task<AccountPayableDTO> GetAccount(long oid)
        {
            try
            {
                var account = await _context.accountspayable.AsNoTracking().FirstOrDefaultAsync(x => x.OID == oid);
                return mapperFactory.getMapper().Map<AccountPayableDTO>(account);
            }
            catch (Exception ex)
            {
                Log.Error(ex);
                return null!;
            }
        }

        public async Task<List<AccountPayableDTO>> GetAccountList(long acccoundoid, long expensetypeoid, DateTime start, DateTime end)
        {
            try
            {
                AccountPayableEntity[] accounts;

                if (expensetypeoid == 0)
                {
                    accounts = await _context.accountspayable.AsNoTracking()
                                       .Where(x => x.AccountOID == acccoundoid && x.DueDate >= start && x.DueDate <= end)
                                       .ToArrayAsync();
                }
                else
                {
                    accounts = await _context.accountspayable.AsNoTracking()
                                       .Where(x => x.AccountOID == acccoundoid && x.ExpenseTypeOID == expensetypeoid && x.DueDate >= start && x.DueDate <= end)
                                       .ToArrayAsync();
                }

                var mapper = mapperFactory.getMapper();
                return accounts
                    .Select(item => mapper.Map<AccountPayableDTO>(item))
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

        public async Task<long> Add(AccountPayableDTO model)
        {
            try
            {
                long id = 0;
                AccountPayableEntity item = new AccountPayableEntity
                {
                    AccountOID = model.AccountOID,
                    DueDate = model.DueDate,
                    Description = model.Description,
                    ExpenseTypeOID = model.ExpenseTypeOID,
                    Payday = model.Payday,
                    AccountValue = model.AccountValue,
                    CreatedBy = model.CreatedBy,
                    CreatedDate = model.CreatedDate
                };

                _context.accountspayable.Add(item);
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

        public async Task<bool> Add(List<AccountPayableDTO> list)
        {
            using (IDbContextTransaction dbTransaction = await _context.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead))
            {
                try
                {
                    foreach (var model in list)
                    {
                        AccountPayableEntity item = new AccountPayableEntity
                        {
                            AccountOID = model.AccountOID,
                            DueDate = model.DueDate,
                            Description = model.Description,
                            ExpenseTypeOID = model.ExpenseTypeOID,
                            Payday = model.Payday,
                            AccountValue = model.AccountValue,
                            CreatedBy = model.CreatedBy,
                            CreatedDate = model.CreatedDate
                        };

                        _context.accountspayable.Add(item);
                        await _context.SaveChangesAsync();
                    }

                    await dbTransaction.CommitAsync();
                    return true;
                }
                catch (Exception ex)
                {
                    Log.Error(ex);
                    await dbTransaction.RollbackAsync();
                    return false;
                }
            }
        }

        public async Task<bool> Delete(long oid)
        {
            try
            {
                var deleted = await _context.accountspayable
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

        public async Task<bool> Update(AccountPayableDTO model)
        {
            try
            {
                bool ret = false;

                var account = await _context.accountspayable.FirstOrDefaultAsync(x => x.OID == model.OID);
                if (account != null)
                {
                    account.AccountOID = model.AccountOID;
                    account.DueDate = model.DueDate;
                    account.Description = model.Description;
                    account.ExpenseTypeOID = model.ExpenseTypeOID;
                    account.Payday = model.Payday;
                    account.AccountValue = model.AccountValue;
                    account.UpdatedBy = model.UpdatedBy;
                    account.UpdatedDate = model.UpdatedDate;

                    if (model.PaymentWithLaunch)
                    {
                        _context.accountsreceivable.Add(new AccountReceivableEntity()
                        {
                            AccountOID = 5, //transfer funds account
                            RevenueTypeOID = 13, //money transfer oid
                            DueDate = DateTime.Now,
                            CreatedBy = model.UpdatedBy,
                            CreatedDate = DateTime.Now,
                            AccountValue = model.AccountValue,
                            Payday = model.Payday
                        });
                    }

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

        public async Task<bool> InsertFromDataSet(string[] lines, string username, long accountOID, DateTime dateFrom, DateTime dateTo)
        {
            if (lines != null)
            {
                var payable_list = await _context.accountspayable.Where(x => x.AccountOID == accountOID &&
                                                                                                    x.DueDate >= dateFrom && x.DueDate <= dateTo)
                                                                    .ToListAsync();

                using (IDbContextTransaction dbTransaction = await _context.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted))
                {
                    try
                    {
                        DateTime import_date = DateTime.Now;
                        decimal import_value = 0;
                        string import_desc = null!;

                        foreach (string line in lines)
                        {
                            string[] items = line.Replace("\"", "").Split(',');
                            import_date = Convert.ToDateTime(items[0]);
                            import_value = Convert.ToDecimal(items[1].Replace(".", ","));
                            import_desc = items[2];
                            if (import_desc.IndexOf("Value Date:") > -1)
                            {
                                string[] desc = import_desc.Split(new string[] { "Value Date:" }, StringSplitOptions.None);
                                import_date = Convert.ToDateTime(desc[1].Trim());
                                import_desc = desc[0].Trim();
                            }

                            if (import_date >= dateFrom && import_date <= dateTo && import_value < 0)
                            {
                                var result = LaunchValid(payable_list, import_date, import_value, import_desc, dateFrom);
                                if (result.Item1)
                                {
                                    if (result.Item2 != null)
                                    {
                                        result.Item2.Payday = DateTime.Now.Date;
                                        result.Item2.UpdatedBy = username;
                                        result.Item2.UpdatedDate = DateTime.Now;

                                        await _context.SaveChangesAsync();
                                    }
                                    else
                                    {
                                        AccountPayableEntity item = new AccountPayableEntity
                                        {
                                            AccountOID = accountOID,
                                            DueDate = import_date,
                                            Description = import_desc,
                                            ExpenseTypeOID = result.Item3,
                                            Payday = DateTime.Now.Date,
                                            AccountValue = (import_value * -1),
                                            CreatedBy = username,
                                            CreatedDate = DateTime.Now
                                        };

                                        _context.accountspayable.Add(item);
                                        await _context.SaveChangesAsync();
                                    }
                                }
                            }

                        }

                        // Save Rows
                        await _context.SaveChangesAsync();
                        await dbTransaction.CommitAsync();

                        return true;
                    }
                    catch (Exception ex)
                    {
                        await dbTransaction.RollbackAsync();
                        Log.Error(ex);
                        return false;
                    }
                }
            }
            else
                return false;
        }

        private Tuple<bool, AccountPayableEntity, long> LaunchValid(List<AccountPayableEntity> payable_list, DateTime import_date,
                                                                    decimal import_value, string import_desc, DateTime from)
        {
            const long EXPENSES_EXTRAS = 9;
            try
            {
                List<string> expenses = new List<string>() { "BUPA AUSTRALIA", "OTF MOSMAN", "TOYOTA FINANCE", "GOOGLE*OPTUS", "STREAMOTION PTY", "ITALKI", "INTERNATIONAL TRANSACTION FEE", "CHEF FILIPE", "TRANSFER TO", "ENERGYAUSTRALIA", "NICHOLAS FISH", "COLES EXPRESS", "COLES", "WOOLWORTHS", "ALDI STORES", "RITCHIES SUPA", "HFM", "HUDSONMEATS", "TPG INTERNET", "STAN.COM.AU", "CHEMIST", "OPAL", "SPOTIFY", "DEFT RENT", "MACROS.COM.AU", "RAIZ INVESTMENT", "NEXTFLIX.COM", "VODAFONE", "PLINE PH", "RAIZ MAINT FEE", "KEGS ON LEGS", "KEGSWAPPA", "KEGLAND DISTRIBUTION", "E-TOLL PH", "METLIFEINSURANCE", "ZURICH LIFE" };
                List<long> oids = new List<long>() { 17, 13, 15, 26, 26, 8, 27, 24, 12, 5, 2, 10, 2, 2, 2, 2, 2, 2, 4, 26, 6, 10, 26, 1, 24, 23, 26, 3, 6, 28, 29, 29, 29, 10, 30, 30 };
                List<string> launched_expenses = new List<string>() { "BUPA MELBOURNE", "ORANGETHEORY GYM", "TOYOTA CH-R", "OPTUS SPORT", "KAYO", "", "", "", "", "ENERGYAUSTRALIA", "", "", "", "", "", "", "", "", "TPG", "STAN BASIC", "", "", "SPOTIFY", "RENT FLAT", "", "", "NEXTFLIX", "VODAFONE", "", "", "", "", "KEGLAND DISTRIBUTION", "", "", "" };

                bool result = false;
                bool found = false;
                AccountPayableEntity entity = null!;
                long expenseoid = 0;
                import_desc = import_desc.ToUpper();
                import_value = import_value * -1;

                for (int i = 0; i < expenses.Count; i++)
                {
                    if (import_desc.IndexOf(expenses[i]) > -1)
                    {
                        entity = payable_list?.OrderByDescending(x => x.DueDate).FirstOrDefault(x => x.Description!.ToUpper().Contains(expenses[i]) && x.AccountValue == import_value && x.DueDate >= from && x.DueDate <= import_date)!;
                        if (entity == null && string.IsNullOrWhiteSpace(launched_expenses[i]) == false)
                            entity = payable_list?.OrderByDescending(x => x.DueDate).FirstOrDefault(x => x.Description!.ToUpper().Contains(launched_expenses[i]) && x.AccountValue == import_value && x.DueDate >= from && x.DueDate <= import_date)!;

                        if (entity != null)
                        {
                            if (entity.Payday != null)
                            {
                                found = true;
                                result = false;
                                expenseoid = 0;
                                entity = null!;
                                break;
                            }
                            else
                            {
                                expenseoid = oids[i];
                                result = true;
                                break;
                            }
                        }
                        else
                        {
                            result = true;
                            expenseoid = oids[i];
                            break;
                        }
                    }
                }

                if (result == false && found == false)
                {
                    result = true;
                    expenseoid = EXPENSES_EXTRAS;
                }

                return new Tuple<bool, AccountPayableEntity, long>(result, entity!, expenseoid);
            }
            catch (Exception ex)
            {
                Log.Error(ex);
                return new Tuple<bool, AccountPayableEntity, long>(false, null!, 0);
            }
        }

        public async Task<List<ExpenseTypeDTO>> GetAllExpenseTotalByMonth(long accountoid, int month, int year)
        {
            try
            {
                List<ExpenseTypeDTO> list = new List<ExpenseTypeDTO>();

                DateTime from = new DateTime(year, month, 1);
                DateTime to = from.AddMonths(1).AddDays(-1);
                var accounts = new long[] { 1, 5, 7, 8 };
                var expenses_oids = new long[] { 1, 42, 52 };

                var accountspay = await _context.accountspayable.AsNoTracking()
                                                    .Where(e => e.AccountOID == accountoid && e.DueDate >= from && e.DueDate <= to && e.Payday != null)
                                                    .ToArrayAsync();

                var accountrepayment_wa = await _context.accountspayable.AsNoTracking()
                                                    .Where(e => accounts.Contains(e.AccountOID) && e.DueDate >= from && e.DueDate <= to && e.Payday != null && e.ExpenseTypeOID == 41)
                                                    .ToArrayAsync();

                var accountrepayment_wa_loan = await _context.accountspayable.AsNoTracking()
                                                    .Where(e => accounts.Contains(e.AccountOID) && e.DueDate >= from && e.DueDate <= to && e.Payday != null && e.ExpenseTypeOID == 42)
                                                    .FirstOrDefaultAsync();

                var accountrepayment_qld = await _context.accountspayable.AsNoTracking()
                                                    .Where(e => accounts.Contains(e.AccountOID) && e.DueDate >= from && e.DueDate <= to && e.Payday != null && e.ExpenseTypeOID == 53)
                                                    .ToArrayAsync();

                var accountrepayment_qld_loan = await _context.accountspayable.AsNoTracking()
                                                    .Where(e => accounts.Contains(e.AccountOID) && e.DueDate >= from && e.DueDate <= to && e.Payday != null && e.ExpenseTypeOID == 52)
                                                    .FirstOrDefaultAsync();

                var accountproprent = await _context.accountspayable.AsNoTracking()
                                                    .Where(e => accounts.Contains(e.AccountOID) && e.DueDate >= from && e.DueDate <= to && e.Payday != null && e.ExpenseTypeOID == 1)
                                                    .ToArrayAsync();

                var expenses_entity = await _context.expenses.AsNoTracking().ToArrayAsync();

                var expensesdto = (from f in expenses_entity
                                   join e in accountspay on f.OID equals e.ExpenseTypeOID
                                   group e by new { f.Name, f.OID } into g
                                   select new ExpenseTypeDTO { OID = g.Key.OID, Name = g.Key.Name, EstimatedValue = g.Sum(p => p.AccountValue) })
                               .ToArray();

                list.AddRange(expensesdto.Where(x => x.EstimatedValue > 0));

                if (accountrepayment_wa?.Any() ?? false)
                {
                    expensesdto = (from f in expenses_entity
                                   join e in accountrepayment_wa on f.OID equals e.ExpenseTypeOID
                                   group e by new { f.Name, f.OID } into g
                                   select new ExpenseTypeDTO { OID = g.Key.OID, Name = g.Key.Name, EstimatedValue = g.Sum(p => p.AccountValue) })
                                   .ToArray();

                    list.AddRange(expensesdto.Where(x => x.EstimatedValue > 0));
                }

                if (accountrepayment_qld?.Any() ?? false)
                {
                    expensesdto = (from f in expenses_entity
                                   join e in accountrepayment_qld on f.OID equals e.ExpenseTypeOID
                                   group e by new { f.Name, f.OID } into g
                                   select new ExpenseTypeDTO { OID = g.Key.OID, Name = g.Key.Name, EstimatedValue = g.Sum(p => p.AccountValue) })
                                   .ToArray();

                    list.AddRange(expensesdto.Where(x => x.EstimatedValue > 0));
                }

                var expenses = expenses_entity.Where(x => expenses_oids.Contains(x.OID))
                                              .Select(x => new { x.OID, x.Name })
                                              .ToArray();

                if (accountrepayment_wa_loan != null)
                {
                    list.Add(new ExpenseTypeDTO()
                    {
                        OID = accountrepayment_wa_loan.ExpenseTypeOID,
                        Name = expenses.FirstOrDefault(x => x.OID == accountrepayment_wa_loan.ExpenseTypeOID)?.Name,
                        EstimatedValue = accountrepayment_wa_loan.AccountValue
                    });
                }

                if (accountrepayment_qld_loan != null)
                {
                    list.Add(new ExpenseTypeDTO()
                    {
                        OID = accountrepayment_qld_loan.ExpenseTypeOID,
                        Name = expenses.FirstOrDefault(x => x.OID == accountrepayment_qld_loan.ExpenseTypeOID)?.Name,
                        EstimatedValue = accountrepayment_qld_loan.AccountValue
                    });
                }

                if (accountproprent?.Length > 0)
                {
                    var total_value = accountproprent.Sum(qld => qld.AccountValue);

                    list.Add(new ExpenseTypeDTO()
                    {
                        OID = accountproprent[0].ExpenseTypeOID,
                        Name = expenses.FirstOrDefault(x => x.OID == accountproprent[0].ExpenseTypeOID)?.Name,
                        EstimatedValue = total_value
                    });
                }

                return list.OrderByDescending(x => x.EstimatedValue).ToList();
            }
            catch (Exception ex)
            {
                Log.Error(ex);
                return null!;
            }
        }
    }
}
