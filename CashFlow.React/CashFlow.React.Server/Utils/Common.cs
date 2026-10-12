using CashFlow.Core.Classes;
using CashFlow.Core.DTO;
using CashFlow.Core.Models;
using CashFlow.DataAccess.Repositories;
using log4net;
using System.Globalization;

namespace CashFlow.React.Server.Utils
{
    public class Common
    {
        public static bool TryDate(string? text, out DateTime date) => DateTime.TryParseExact(text,
            "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out date);
        public static string Iso(DateTime date) => date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        public static DateTime LocalNow => TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow,
                    TimeZoneInfo.FindSystemTimeZoneById("Australia/Sydney")).DateTime;

        public static int RECEIVABLE_MARY_PAYMENT_OID = 1;
        public static int RECEIVABLE_AIRES_PAYMENT_OID = 12;

        private static ILog Log = LogManager.GetLogger("Common");

        private static AESEncryptor logAES = null!;

        public static Tuple<long, long, DateTime, DateTime> GetFixedFilterValues(AccountEntityRepository accountRepo)
        {
            try
            {
                DateTime launchFrom = (DateTime)accountRepo.GetPeriodStartFromMainSalary()!;
                return new Tuple<long, long, DateTime, DateTime>(accountRepo.MAIN_REVENUE_SALARY_OID, accountRepo.MAIN_ACCOUNT_OID, launchFrom, launchFrom.AddDays(6));
            }
            catch (Exception ex)
            {
                Log.Error(ex);
                return null!;
            }
        }

        public static List<AccountDTO> GetAccountList(AccountEntityRepository accountRepo)
        {
            try
            {
                return accountRepo.GetAccountList().Where(x => !x.Name.Contains("INACTIVE")).ToList();
            }
            catch (Exception ex)
            {
                Log.Error(ex);
                return new List<AccountDTO>();
            }
        }

        public static BalanceModel GetBalanceDetailsModel(AccountEntityRepository accountRepo, long acccoundoid, long expensetypeoid,
                                                          long revenuetypeoid, DateTime launchstart, DateTime launchend)
        {
            try
            {
                BalanceModel balance_model = new BalanceModel();

                balance_model.PreviousBalance = accountRepo.GetAccountPreviousBalance(acccoundoid, launchstart);
                balance_model.FinalBalance = accountRepo.GetAccountFinalBalance(acccoundoid, launchend);

                var total = accountRepo.GetAccountTotalForPeriod(acccoundoid, 0, expensetypeoid, revenuetypeoid, launchstart, launchend);

                balance_model.TotalExpenses = total.Item1;
                balance_model.TotalRevenues = total.Item2;

                return balance_model;
            }
            catch (Exception ex)
            {
                Log.Error(ex);
                return new BalanceModel();
            }
        }

        private static void AesInitialize()
        {
            if (logAES == null)
            {
                logAES = new AESEncryptor();
            }
        }

        public static string AesEncrypt(string text)
        {
            AesInitialize();
            return logAES.Encrypt(text);
        }

        public static string AesDecrypt(string text)
        {
            AesInitialize();
            return logAES.Decrypt(text);
        }

        public static string GetRootPathFile(IWebHostEnvironment _environment)
        {
            try
            {
                return Path.Combine(_environment.ContentRootPath, "Files");
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}
