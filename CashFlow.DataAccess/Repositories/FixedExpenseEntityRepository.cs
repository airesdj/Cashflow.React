using CashFlow.Core.DTO;
using CashFlow.DataAccess.Entities;
using CashFlow.DataAccess.Repositories.Base;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CashFlow.DataAccess.Repositories
{
    public class FixedExpenseEntityRepository : BaseRepository
    {
        private readonly Context _context;
        public FixedExpenseEntityRepository(Context context, ILogger<BaseRepository> logger) : base(logger)
        {
            _context = context;
        }

        protected override void Dispose(bool disposing)
            => base.Dispose(disposing);


        public async Task<FixedExpenseDTO> GetFixedExpense(long oid)
        {
            try
            {
                var entity = await _context.fixedexpenses.AsNoTracking().FirstOrDefaultAsync(x => x.OID == oid);
                return mapperFactory.getMapper().Map<FixedExpenseDTO>(entity);
            }
            catch (Exception ex)
            {
                Log.Error(ex);
                return null!;
            }
        }

        public async Task<List<FixedExpenseDTO>> GetFixedExpenseList()
        {
            try
            {
                var list = await _context.fixedexpenses.AsNoTracking()
                        .Select(o => new FixedExpenseDTO
                        {
                            OID = o.OID,
                            ExpenseTypeOID = o.ExpenseTypeOID,
                            Description = o.Description,
                            IsFortnightlyExpense = o.IsFortnightlyExpense,
                            IsMonthlyExpense = o.IsMonthlyExpense,
                            AdditionalDays = o.AdditionalDays,
                            DayOfMonth = o.DayOfMonth,
                            ExpenseValue = o.ExpenseValue,
                            IsDeleted = o.IsDeleted
                        })
                        .OrderBy(o => o.ExpenseTypeOID).ThenBy(o => o.Description)
                        .ToListAsync();

                return list;
            }
            catch (Exception ex)
            {
                Log.Error(ex);
                return null!;
            }
        }

        public async Task<long> Add(FixedExpenseDTO model)
        {
            try
            {
                long id = 0;
                FixedExpenseEntity item = new FixedExpenseEntity
                {
                    ExpenseTypeOID = model.ExpenseTypeOID,
                    Description = model.Description,
                    IsFortnightlyExpense = model.IsFortnightlyExpense,
                    IsMonthlyExpense = model.IsMonthlyExpense,
                    AdditionalDays = model.AdditionalDays,
                    DayOfMonth = model.DayOfMonth,
                    ExpenseValue = model.ExpenseValue,
                    IsDeleted = false
                };

                _context.fixedexpenses.Add(item);
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
                var updated = await _context.fixedexpenses
                                .Where(x => x.OID == oid)
                                .ExecuteUpdateAsync(updates => updates
                                    .SetProperty(h => h.IsDeleted, _ => true));

                if (updated == 0)
                    Log.Error("Fixed expense NOT found.");

                return updated > 0 ? true : false;
            }
            catch (Exception ex)
            {
                Log.Error(ex);
                return false;
            }
        }

        public async Task<bool> Update(FixedExpenseDTO model)
        {
            try
            {
                bool ret = false;

                var FixedExpense = await _context.fixedexpenses.FirstOrDefaultAsync(x => x.OID == model.OID);
                if (FixedExpense != null)
                {
                    FixedExpense.ExpenseTypeOID = model.ExpenseTypeOID;
                    FixedExpense.Description = model.Description;
                    FixedExpense.IsFortnightlyExpense = model.IsFortnightlyExpense;
                    FixedExpense.IsMonthlyExpense = model.IsMonthlyExpense;
                    FixedExpense.AdditionalDays = model.AdditionalDays;
                    FixedExpense.DayOfMonth = model.DayOfMonth;
                    FixedExpense.ExpenseValue = model.ExpenseValue;
                    FixedExpense.IsDeleted = false;

                    await _context.SaveChangesAsync();

                    ret = true;
                }
                else
                    Log.Error("Fixed expense NOT found.");

                return ret;
            }
            catch (Exception ex)
            {
                Log.Error(ex);
                return false;
            }
        }

        public async Task<bool> IsFortnightlyLaunched(DateTime datelimit)
        {
            long expenseoid = 1;
            return await _context.accountspayable.AsNoTracking().AnyAsync(x => x.ExpenseTypeOID == expenseoid && x.DueDate >= datelimit);
        }

        public async Task<List<ExpenseTypeDTO>> GetAllExpenseTypeTotal()
        {
            try
            {
                List<ExpenseTypeDTO> list = new List<ExpenseTypeDTO>();

                var excluding = new long[] { 46, 47, 48, 49 };

                var expenses_montly = await (from f in _context.fixedexpenses.AsNoTracking()
                                             join e in _context.expenses.AsNoTracking() on f.ExpenseTypeOID equals e.OID
                                             where !f.IsDeleted && f.IsMonthlyExpense && !excluding.Contains(e.OID)
                                             group f by new { e.OID, e.Name } into g
                                             select new ExpenseTypeDTO { Name = g.Key.Name, EstimatedValue = g.Sum(p => p.ExpenseValue) ?? 0 }).ToArrayAsync();

                list.AddRange(expenses_montly.Where(x => x.EstimatedValue > 0));

                var expenses_fortn = await (from f in _context.fixedexpenses.AsNoTracking()
                                            join e in _context.expenses.AsNoTracking() on f.ExpenseTypeOID equals e.OID
                                            where !f.IsDeleted && f.IsFortnightlyExpense && !excluding.Contains(e.OID)
                                            group f by new { e.Name } into g
                                            select new ExpenseTypeDTO { Name = g.Key.Name, EstimatedValue = (g.Sum(p => p.ExpenseValue) * 2) ?? 0 }).ToArrayAsync();

                foreach (var expense in expenses_fortn.Where(x => x.EstimatedValue > 0).ToArray())
                {
                    var item = list.FirstOrDefault(x => x.Name == expense.Name);

                    if (item == null)
                        list.Add(expense);
                    else
                        item.EstimatedValue += expense.EstimatedValue;
                }

                var expenses_week = await (from f in _context.fixedexpenses.AsNoTracking()
                                           join e in _context.expenses.AsNoTracking() on f.ExpenseTypeOID equals e.OID
                                           where !f.IsDeleted && !f.IsMonthlyExpense && !excluding.Contains(e.OID)
                                                 && !f.IsFortnightlyExpense
                                           group f by new { e.Name } into g
                                           select new ExpenseTypeDTO { Name = g.Key.Name, EstimatedValue = (g.Sum(p => p.ExpenseValue) * 4) ?? 0 }).ToArrayAsync();

                foreach (var expense in expenses_week.Where(x => x.EstimatedValue > 0).ToArray())
                {
                    var item = list.FirstOrDefault(x => x.Name == expense.Name);

                    if (item == null)
                        list.Add(expense);
                    else
                        item.EstimatedValue += expense.EstimatedValue;
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
