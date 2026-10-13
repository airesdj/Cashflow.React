using CashFlow.Core.DTO;
using CashFlow.DataAccess.Repositories.Base;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;

namespace CashFlow.DataAccess.Repositories
{
    public class UserEntityRepository : BaseRepository
    {
        private readonly Context _context;
        public UserEntityRepository(Context context, ILogger<BaseRepository> logger) : base(logger)
        {
            _context = context;
        }

        protected override void Dispose(bool disposing)
            => base.Dispose(disposing);


        public UserDTO Authentication(string username, string password)
        {
            try
            {
                string encPassword = EncryptSha256(password);

                return mapperFactory.getMapper().Map<UserDTO>(_context.users.AsNoTracking()
                                                                       .FirstOrDefault(x => x.UserName.ToLower() == username.ToLower() &&
                                                                                            x.Password == encPassword));
            }
            catch (Exception ex)
            {
                Log.Error(ex);
                return null!;
            }
        }

        private string EncryptSha256(string input)
        {
            string ret = null!;
            StringBuilder builder = new StringBuilder();

            using (SHA256 hash = SHA256.Create())
            {
                byte[] result = hash.ComputeHash(Encoding.UTF8.GetBytes(input));
                result.ToList()
                      .ForEach(b => builder.Append(b.ToString("x2")));
            }
            ret = builder.ToString();

            builder.Clear();
            builder = null!;

            return ret;
        }
    }
}
