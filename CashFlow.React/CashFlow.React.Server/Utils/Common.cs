using CashFlow.Core.Classes;
using CashFlow.Core.DTO;
using CashFlow.Core.Models;
using CashFlow.DataAccess.Repositories;
using System.Globalization;

namespace CashFlow.React.Server.Utils
{
    public class Common
    {
        public const int TOTAL_ITEMS_PER_PAGE = 10;
        public static bool TryDate(string? text, out DateTime date) => DateTime.TryParseExact(text,
            "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out date);
        public static string Iso(DateTime date) => date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        public static DateTime LocalNow => TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow,
                    TimeZoneInfo.FindSystemTimeZoneById("Australia/Sydney")).DateTime;

        public static int RECEIVABLE_MARY_PAYMENT_OID = 1;
        public static int RECEIVABLE_AIRES_PAYMENT_OID = 12;

        private static AESEncryptor logAES = null!;

        private static ILogger? logger;

        public static void ConfigureLogger(ILoggerFactory loggerFactory)
        {
            logger = loggerFactory.CreateLogger("Common");
        }

        public static async Task<Tuple<long, long, DateTime, DateTime>> GetFixedFilterValues(AccountEntityRepository accountRepo)
        {
            try
            {
                DateTime launchFrom = await accountRepo.GetPeriodStartFromMainSalary()
                    ?? throw new InvalidOperationException("Mary salary period start was not found.");
                return new Tuple<long, long, DateTime, DateTime>(accountRepo.MAIN_REVENUE_SALARY_OID, accountRepo.MAIN_ACCOUNT_OID, launchFrom, launchFrom.AddDays(6));
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "GetFixedFilterValues()");
                return null!;
            }
        }

        public static async Task<List<AccountDTO>> GetAccountList(AccountEntityRepository accountRepo)
        {
            try
            {
                return (await accountRepo.GetAccountList()).Where(x => !x.Name.Contains("INACTIVE")).ToList();
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "GetAccountList()");
                return new List<AccountDTO>();
            }
        }

        public static async Task<BalanceModel> GetBalanceDetailsModel(AccountEntityRepository accountRepo, long acccoundoid, long expensetypeoid,
                                                          long revenuetypeoid, DateTime launchstart, DateTime launchend)
        {
            try
            {
                BalanceModel balance_model = new BalanceModel();

                balance_model.PreviousBalance = await accountRepo.GetAccountPreviousBalance(acccoundoid, launchstart);
                balance_model.FinalBalance = await accountRepo.GetAccountFinalBalance(acccoundoid, launchend);

                var total = await accountRepo.GetAccountTotalForPeriod(acccoundoid, 0, expensetypeoid, revenuetypeoid, launchstart, launchend);

                balance_model.TotalExpenses = total.Item1;
                balance_model.TotalRevenues = total.Item2;

                return balance_model;
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "GetBalanceDetailsModel()");
                return new BalanceModel();
            }
        }

        private static void AesInitialize()
            => logAES ??= new AESEncryptor();


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
