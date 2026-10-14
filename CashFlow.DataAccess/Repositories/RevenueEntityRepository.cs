using CashFlow.Core.DTO;
using CashFlow.DataAccess.Entities;
using CashFlow.DataAccess.Repositories.Base;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CashFlow.DataAccess.Repositories
{
    public class RevenueEntityRepository : BaseRepository
    {
        private readonly Context _context;
        public RevenueEntityRepository(Context context, ILogger<BaseRepository> logger) : base(logger)
        {
            _context = context;
        }

        protected override void Dispose(bool disposing)
            => base.Dispose(disposing);


        public async Task<RevenueTypeDTO> GetRevenue(long oid)
        {
            try
            {
                var entity = await _context.revenues.AsNoTracking().FirstOrDefaultAsync(x => x.OID == oid);
                return mapperFactory.getMapper().Map<RevenueTypeDTO>(entity);
            }
            catch (Exception ex)
            {
                Log.Error(ex);
                return null!;
            }
        }

        public Task<bool> GetRevenueByName(string name, long oid = 0)
            => _context.revenues.AsNoTracking().AnyAsync(x => x.Name == name && (oid == 0 || x.OID != oid));

        public async Task<List<RevenueTypeDTO>> GetRevenueList()
        {
            try
            {
                var revenues = await _context.revenues.AsNoTracking().OrderBy(o => o.Name)
                                        .ToArrayAsync();

                var mapper = mapperFactory.getMapper();

                return revenues.Select(item => mapper.Map<RevenueTypeDTO>(item)).ToList();
            }
            catch (Exception ex)
            {
                Log.Error(ex);
                return null!;
            }
        }

        public async Task<long> Add(RevenueTypeDTO model)
        {
            try
            {
                long id = 0;
                RevenueTypeEntity item = new RevenueTypeEntity
                {
                    Name = model.Name,
                };

                _context.revenues.Add(item);
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
                var deleted = await _context.revenues
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

        public async Task<bool> Update(RevenueTypeDTO model)
        {
            try
            {
                var updated = await _context.revenues
                                .Where(x => x.OID == model.OID)
                                .ExecuteUpdateAsync(updates => updates
                                    .SetProperty(h => h.Name, _ => model.Name));

                if (updated == 0)
                    Log.Error("Revenue NOT found.");

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
