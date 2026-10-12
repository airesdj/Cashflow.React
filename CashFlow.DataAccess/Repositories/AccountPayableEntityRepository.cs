using CashFlow.Core.DTO;
using CashFlow.DataAccess.Entities;
using CashFlow.DataAccess.Repositories.Base;
using log4net;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System.Data;

namespace CashFlow.DataAccess.Repositories
{
    public class AccountPayableEntityRepository : BaseRepository
    {
        private readonly Context _context;
        public AccountPayableEntityRepository(Context context)
        {
            Log = LogManager.GetLogger("AccountPayableEntityRepository");
            _context = context;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                GC.SuppressFinalize(this);
                base.Dispose(disposing);
            }
        }

        public List<AccountPayableDTO> GetAllAccountPayableByDay(DateTime day)
        {
            try
            {
                List<AccountPayableDTO> list = new List<AccountPayableDTO>();

                list = (from a in _context.accountspayable.AsNoTracking()
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
                        }).ToList();

                return list;
            }
            catch (Exception ex)
            {
                Log.Error(ex);
                return null!;
            }
        }

        public AccountPayableDTO GetAccount(long oid)
        {
            try
            {
                return mapperFactory.getMapper().Map<AccountPayableDTO>(_context.accountspayable.AsNoTracking().FirstOrDefault(x => x.OID == oid));
            }
            catch (Exception ex)
            {
                Log.Error(ex);
                return null!;
            }
        }

        public List<AccountPayableDTO> GetAccountList(long acccoundoid, long expensetypeoid, DateTime start, DateTime end)
        {
            try
            {
                List<AccountPayableDTO> list = new List<AccountPayableDTO>();

                if (expensetypeoid == 0)
                {
                    _context.accountspayable.AsNoTracking()
                                       .Where(x => x.AccountOID == acccoundoid && x.DueDate >= start && x.DueDate <= end)
                                       .ToList()
                                       .ForEach(account => list.Add(mapperFactory.getMapper().Map<AccountPayableDTO>(account)));
                }
                else
                {
                    _context.accountspayable.AsNoTracking()
                                       .Where(x => x.AccountOID == acccoundoid && x.ExpenseTypeOID == expensetypeoid && x.DueDate >= start && x.DueDate <= end)
                                       .ToList()
                                       .ForEach(account => list.Add(mapperFactory.getMapper().Map<AccountPayableDTO>(account)));
                }

                return list.OrderBy(x => x.DueDate).ThenBy(x => x.Payday).ToList();
            }
            catch (Exception ex)
            {
                Log.Error(ex);
                return null!;
            }
        }

        public long Add(AccountPayableDTO model)
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

        public bool Add(List<AccountPayableDTO> list)
        {
            using (IDbContextTransaction dbTransaction = _context.Database.BeginTransaction(IsolationLevel.RepeatableRead))
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
                        _context.SaveChanges();
                    }

                    dbTransaction.Commit();
                    return true;
                }
                catch (Exception ex)
                {
                    Log.Error(ex);
                    dbTransaction.Rollback();
                    return false;
                }
            }
        }

        public bool Delete(long oid)
        {
            AccountPayableEntity? account = null;

            try
            {
                account = _context.accountspayable.FirstOrDefault(x => x.OID == oid);
                if (account != null)
                {
                    _context.accountspayable.Attach(account);
                    _context.accountspayable.Remove(account);
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

        public bool Update(AccountPayableDTO model)
        {
            try
            {
                bool ret = false;

                var account = _context.accountspayable.FirstOrDefault(x => x.OID == model.OID);
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

        public bool InsertFromDataSet(string[] lines, string username, long accountOID, DateTime dateFrom, DateTime dateTo)
        {
            if (lines != null)
            {
                List<AccountPayableEntity> payable_list = _context.accountspayable.Where(x => x.AccountOID == accountOID && x.DueDate >= dateFrom && x.DueDate <= dateTo).ToList();

                using (IDbContextTransaction dbTransaction = _context.Database.BeginTransaction(IsolationLevel.ReadCommitted))
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

                                        _context.Entry(result.Item2).State = EntityState.Modified;
                                        _context.SaveChanges();
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
                                        _context.SaveChanges();
                                    }
                                }
                            }

                        }

                        // Save Rows
                        _context.SaveChanges();
                        dbTransaction.Commit();

                        return true;
                    }
                    catch (Exception ex)
                    {
                        dbTransaction.Rollback();
                        Log.Error(ex);
                        return false;
                    }
                }
            }
            else
                return false;
        }

        private Tuple<bool, AccountPayableEntity, long> LaunchValid(List<AccountPayableEntity> payable_list, DateTime import_date, decimal import_value, string import_desc, DateTime from)
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

        public List<ExpenseTypeDTO> GetAllExpenseTotalByMonth(long accountoid, int month, int year)
        {
            try
            {
                List<ExpenseTypeDTO> list = new List<ExpenseTypeDTO>();

                DateTime from = new DateTime(year, month, 1);
                DateTime to = from.AddMonths(1).AddDays(-1);
                var accounts = new long[] { 1, 5, 7, 8 };
                var expenses_oids = new long[] { 1, 42, 52 };

                var accountspay = _context.accountspayable.AsNoTracking()
                                                    .Where(e => e.AccountOID == accountoid && e.DueDate >= from && e.DueDate <= to && e.Payday != null);

                var accountrepayment_wa = _context.accountspayable.AsNoTracking()
                                                    .Where(e => accounts.Contains(e.AccountOID) && e.DueDate >= from && e.DueDate <= to && e.Payday != null && e.ExpenseTypeOID == 41);

                var accountrepayment_wa_loan = _context.accountspayable.AsNoTracking()
                                                    .Where(e => accounts.Contains(e.AccountOID) && e.DueDate >= from && e.DueDate <= to && e.Payday != null && e.ExpenseTypeOID == 42)
                                                    .FirstOrDefault();

                var accountrepayment_qld = _context.accountspayable.AsNoTracking()
                                                    .Where(e => accounts.Contains(e.AccountOID) && e.DueDate >= from && e.DueDate <= to && e.Payday != null && e.ExpenseTypeOID == 53);

                var accountrepayment_qld_loan = _context.accountspayable.AsNoTracking()
                                                    .Where(e => accounts.Contains(e.AccountOID) && e.DueDate >= from && e.DueDate <= to && e.Payday != null && e.ExpenseTypeOID == 52)
                                                    .FirstOrDefault();

                var accountproprent = _context.accountspayable.AsNoTracking()
                                                    .Where(e => accounts.Contains(e.AccountOID) && e.DueDate >= from && e.DueDate <= to && e.Payday != null && e.ExpenseTypeOID == 1)
                                                    .ToList();

                list.AddRange((from f in _context.expenses.AsNoTracking()
                               join e in accountspay on f.OID equals e.ExpenseTypeOID
                               group e by new { f.Name, f.OID } into g
                               select new ExpenseTypeDTO { OID = g.Key.OID, Name = g.Key.Name, EstimatedValue = g.Sum(p => p.AccountValue) })
                               .ToList().Where(x => x.EstimatedValue > 0).ToList());

                if (accountrepayment_wa?.Any() ?? false)
                {
                    list.AddRange((from f in _context.expenses.AsNoTracking()
                                   join e in accountrepayment_wa on f.OID equals e.ExpenseTypeOID
                                   group e by new { f.Name, f.OID } into g
                                   select new ExpenseTypeDTO { OID = g.Key.OID, Name = g.Key.Name, EstimatedValue = g.Sum(p => p.AccountValue) })
                                   .ToList().Where(x => x.EstimatedValue > 0).ToList());
                }

                if (accountrepayment_qld?.Any() ?? false)
                {
                    list.AddRange((from f in _context.expenses.AsNoTracking()
                                   join e in accountrepayment_qld on f.OID equals e.ExpenseTypeOID
                                   group e by new { f.Name, f.OID } into g
                                   select new ExpenseTypeDTO { OID = g.Key.OID, Name = g.Key.Name, EstimatedValue = g.Sum(p => p.AccountValue) })
                                   .ToList().Where(x => x.EstimatedValue > 0).ToList());
                }

                var expenses = _context.expenses.Where(x => expenses_oids.Contains(x.OID))
                                                .Select(x => new { x.OID, x.Name }).ToArray();

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

                if (accountproprent?.Count > 0)
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
