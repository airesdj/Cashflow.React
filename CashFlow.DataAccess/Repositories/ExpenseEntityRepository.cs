using CashFlow.Core.DTO;
using CashFlow.DataAccess.Entities;
using CashFlow.DataAccess.Repositories.Base;
using log4net;
using Microsoft.EntityFrameworkCore;

namespace CashFlow.DataAccess.Repositories
{
    public class ExpenseEntityRepository : BaseRepository
    {
        private readonly Context _context;
        public ExpenseEntityRepository(Context context)
        {
            Log = LogManager.GetLogger("AccountEntityRepository");
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

        public ExpenseTypeDTO GetExpenseByName(string name, long? oid = null)
        {
            var item = _context.expenses.AsNoTracking().Where(x => x.Name == name && (oid == null || x.OID != oid)).FirstOrDefault();
            if (item != null)
                return mapperFactory.getMapper().Map<ExpenseTypeDTO>(item);
            else
                return null!;
        }

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
