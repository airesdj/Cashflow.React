using CashFlow.Core.DTO;
using CashFlow.DataAccess.Entities;
using CashFlow.DataAccess.Repositories.Base;
using log4net;
using Microsoft.EntityFrameworkCore;

namespace CashFlow.DataAccess.Repositories
{
    public class RevenueEntityRepository : BaseRepository
    {
        private readonly Context _context;
        public RevenueEntityRepository(Context context)
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

        public RevenueTypeDTO GetRevenue(long oid)
        {
            try
            {
                return mapperFactory.getMapper().Map<RevenueTypeDTO>(_context.revenues.AsNoTracking().FirstOrDefault(x => x.OID == oid));
            }
            catch (Exception ex)
            {
                Log.Error(ex);
                return null!;
            }
        }

        public RevenueTypeDTO GetRevenueByName(string name, long? oid = null)
        {
            var item = _context.revenues.AsNoTracking().Where(x => x.Name == name && (oid == null || x.OID != oid)).FirstOrDefault();
            if (item != null)
                return mapperFactory.getMapper().Map<RevenueTypeDTO>(item);
            else
                return null!;
        }

        public List<RevenueTypeDTO> GetRevenueList()
        {
            try
            {
                List<RevenueTypeDTO> list = new List<RevenueTypeDTO>();

                foreach (var item in _context.revenues.AsNoTracking().OrderBy(o => o.Name))
                    list.Add(mapperFactory.getMapper().Map<RevenueTypeDTO>(item));

                return list;
            }
            catch (Exception ex)
            {
                Log.Error(ex);
                return null!;
            }
        }

        public long Add(RevenueTypeDTO model)
        {
            try
            {
                long id = 0;
                RevenueTypeEntity item = new RevenueTypeEntity
                {
                    Name = model.Name,
                };

                _context.revenues.Add(item);
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
            RevenueTypeEntity? revenue = null;

            try
            {
                revenue = _context.revenues.FirstOrDefault(o => o.OID == oid);
                if (revenue != null)
                {
                    _context.revenues.Attach(revenue);
                    _context.revenues.Remove(revenue);
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
                revenue = null;
            }
        }

        public bool Update(RevenueTypeDTO model)
        {
            try
            {
                bool ret = false;

                var revenue = _context.revenues.FirstOrDefault(x => x.OID == model.OID);
                if (revenue != null)
                {
                    revenue.Name = model.Name;

                    _context.Entry(revenue).State = EntityState.Modified;
                    _context.SaveChanges();

                    ret = true;
                }
                else
                    Log.Error("Revenue NOT found.");

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
