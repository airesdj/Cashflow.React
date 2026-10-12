using CashFlow.DataAccess.Classes;

namespace CashFlow.DataAccess.Utils
{
    public class ConnectionString
    {
        public static string GetConnectionString_Host() => AppConfig.DbHost;
        public static string GetConnectionString_UserID() => Common.AesDecrypt(AppConfig.DbUserId);
        public static string GetConnectionString_PassWD() => Common.AesDecrypt(AppConfig.DbPassword);
        public static string GetConnectionString_Database() => Common.AesDecrypt(AppConfig.DbName);
    }
}
