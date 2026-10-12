using CashFlow.Core.Classes;
using CashFlow.DataAccess.Utils;

namespace CashFlow.DataAccess.Classes
{
    public class AppSettings
    {
        public ConnectionStringSettings ConnectionStringSettings { get; set; } = new();
        public string ReceivableAiresPaymentValue { get; set; } = string.Empty;
        public string ReceivableMarysPaymentValue { get; set; } = string.Empty;
    }

    public class ConnectionStringSettings
    {
        public string Host { get; set; } = string.Empty;
        public string Port { get; set; } = "0";
        public string UserId { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string DatabaseName { get; set; } = string.Empty;
    }

    public static class AppConfig
    {
        private static AppSettings _settings = null!;
        private static string? _dbConnectionString = null;

        public static void Init(AppSettings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }


        // ConnectionStringSetting
        public static string DbHost => _settings.ConnectionStringSettings.Host;
        public static int DbPort => _settings.ConnectionStringSettings.Port == null ? 3306 : Convert.ToInt32(_settings.ConnectionStringSettings.Port);
        public static string DbUserId => _settings.ConnectionStringSettings.UserId;
        public static string DbPassword => _settings.ConnectionStringSettings.Password;
        public static string DbName => _settings.ConnectionStringSettings.DatabaseName;

        public static string DbConnectionString_Cashflow
        {
            get
            {
                if (_dbConnectionString == null)
                    _dbConnectionString = GetStringConnection(DbName);

                return _dbConnectionString;
            }
        }

        // URL
        public static decimal Settings_ReceivableAiresPaymentValue => Convert.ToDecimal(_settings.ReceivableAiresPaymentValue);
        public static decimal Settings_ReceivableMarysPaymentValue => Convert.ToDecimal(_settings.ReceivableMarysPaymentValue);

        private static string GetStringConnection(string dbName)
        {
            using (StringUtils builder = new StringUtils())
            {
                return builder.Append("server=").Append(DbHost)
                            .Append(";port=").Append(DbPort)
                            .Append(";user id=").Append(Common.AesDecrypt(DbUserId))
                            .Append(";password=").Append(Common.AesDecrypt(DbPassword))
                            .Append(";database=").Append(Common.AesDecrypt(dbName))
                            .Append(";SslMode=Required;TlsVersion=Tls12,Tls13;Pooling=True;Minimum Pool Size=0;Maximum Pool Size=200")
                            .Append(";Connection Timeout=30;Default Command Timeout=30")
                            .Append(";AllowUserVariables=True;AllowPublicKeyRetrieval=False;").ToString();
            }
        }
    }
}
