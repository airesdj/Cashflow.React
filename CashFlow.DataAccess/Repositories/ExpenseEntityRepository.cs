using CashFlow.Core.DTO;
using CashFlow.DataAccess.Entities;
using CashFlow.DataAccess.Repositories.Base;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CashFlow.DataAccess.Repositories
{
    public class ExpenseEntityRepository : BaseRepository
    {
        private readonly Context _context;
        public ExpenseEntityRepository(Context context, ILogger<BaseRepository> logger) : base(logger)
        {
            _context = context;
        }

        protected override void Dispose(bool disposing)
            => base.Dispose(disposing);


        public async Task<ExpenseTypeDTO> GetExpense(long oid)
        {
            try
            {
                var enity = await _context.expenses.AsNoTracking().FirstOrDefaultAsync(x => x.OID == oid);
                return mapperFactory.getMapper().Map<ExpenseTypeDTO>(enity);
            }
            catch (Exception ex)
            {
                Log.Error(ex);
                return null!;
            }
        }

        public Task<bool> GetExpenseByName(string name, long oid = 0)
            => _context.expenses.AsNoTracking().AnyAsync(x => x.Name == name && (oid == 0 || x.OID != oid));

        public async Task<List<ExpenseTypeDTO>> GetExpenseList()
        {
            try
            {
                var list = await _context.expenses.AsNoTracking()
                        .Select(o => new ExpenseTypeDTO
                        {
                            OID = o.OID,
                            Name = o.Name,
                            EstimatedValue = o.EstimatedValue ?? 0
                        })
                        .OrderBy(o => o.Name)
                        .ToListAsync();

                return list;
            }
            catch (Exception ex)
            {
                Log.Error(ex);
                return null!;
            }
        }

        public async Task<long> Add(ExpenseTypeDTO model)
        {
            try
            {
                long id = 0;
                ExpenseTypeEntity item = new ExpenseTypeEntity
                {
                    Name = model.Name,
                    EstimatedValue = model.EstimatedValue
                };

                _context.expenses.Add(item);
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
                var deleted = await _context.expenses
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

        public async Task<bool> Update(ExpenseTypeDTO model)
        {
            try
            {
                var updated = await _context.expenses
                                .Where(x => x.OID == model.OID)
                                .ExecuteUpdateAsync(updates => updates
                                    .SetProperty(h => h.Name, _ => model.Name)
                                    .SetProperty(h => h.EstimatedValue, _ => model.EstimatedValue));

                if (updated == 0)
                    Log.Error("Expense NOT found.");

                return updated > 0 ? true : false;
            }
            catch (Exception ex)
            {
                Log.Error(ex);
                return false;
            }
        }
    }
}
