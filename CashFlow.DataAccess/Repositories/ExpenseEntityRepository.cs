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


        public ExpenseTypeDTO GetExpense(long oid)
        {
            try
            {
                return mapperFactory.getMapper().Map<ExpenseTypeDTO>(_context.expenses.AsNoTracking().FirstOrDefault(x => x.OID == oid));
            }
            catch (Exception ex)
            {
                Log.Error(ex);
                return null!;
            }
        }

        public bool GetExpenseByName(string name, long oid = 0)
            => _context.expenses.AsNoTracking().Any(x => x.Name == name && (oid == 0 || x.OID != oid));

        public List<ExpenseTypeDTO> GetExpenseList()
        {
            try
            {
                List<ExpenseTypeDTO> list = new List<ExpenseTypeDTO>();

                list = _context.expenses.AsNoTracking()
                        .Select(o => new ExpenseTypeDTO
                        {
                            OID = o.OID,
                            Name = o.Name,
                            EstimatedValue = o.EstimatedValue ?? 0
                        })
                        .OrderBy(o => o.Name)
                        .ToList();

                return list;
            }
            catch (Exception ex)
            {
                Log.Error(ex);
                return null!;
            }
        }

        public long Add(ExpenseTypeDTO model)
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
            ExpenseTypeEntity? expense = null;
            try
            {
                expense = _context.expenses.FirstOrDefault(o => o.OID == oid);
                if (expense != null)
                {
                    _context.expenses.Attach(expense);
                    _context.expenses.Remove(expense);
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
                expense = null;
            }
        }

        public bool Update(ExpenseTypeDTO model)
        {
            try
            {
                bool ret = false;

                var expense = _context.expenses.FirstOrDefault(x => x.OID == model.OID);
                if (expense != null)
                {
                    expense.Name = model.Name;
                    expense.EstimatedValue = model.EstimatedValue;

                    _context.Entry(expense).State = EntityState.Modified;
                    _context.SaveChanges();

                    ret = true;
                }
                else
                    Log.Error("Expense NOT found.");

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
