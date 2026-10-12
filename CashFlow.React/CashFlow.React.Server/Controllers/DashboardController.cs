using CashFlow.DataAccess.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;

namespace CashFlow.React.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public class DashboardController : ControllerBase
    {
        private readonly AccountEntityRepository accountRepo;
        private readonly AccountPayableEntityRepository payableRepo;
        private readonly FixedExpenseEntityRepository fixedRepo;
        private readonly ILogger<DashboardController> logger;
        private readonly DashboardSettings settings;

        public DashboardController(AccountEntityRepository accountRepo,
            AccountPayableEntityRepository payableRepo, FixedExpenseEntityRepository fixedRepo,
            IConfiguration configuration, ILogger<DashboardController> logger)
        {
            this.accountRepo = accountRepo;
            this.payableRepo = payableRepo;
            this.fixedRepo = fixedRepo;
            this.logger = logger;
            settings = configuration.GetSection("Dashboard").Get<DashboardSettings>() ?? new();
        }

        [HttpGet]
        public IActionResult Get([FromQuery] string? mes_ano)
        {
            try
            {
                var timeZone = TimeZoneInfo.FindSystemTimeZoneById(settings.TimeZoneId);
                var today = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, timeZone).Date;
                var currentMonth = new DateTime(today.Year, today.Month, 1);
                var selectedMonth = currentMonth;
                if (!string.IsNullOrWhiteSpace(mes_ano))
                {
                    if (!DateTime.TryParseExact(mes_ano, new[] { "M-yyyy", "MM-yyyy" },
                        CultureInfo.InvariantCulture, DateTimeStyles.None, out selectedMonth))
                        return BadRequest(new { message = "Invalid month. Use month-year, for example 10-2026." });
                    selectedMonth = new DateTime(selectedMonth.Year, selectedMonth.Month, 1);
                    if (selectedMonth < currentMonth.AddMonths(-11) || selectedMonth > currentMonth)
                        return BadRequest(new { message = "Choose one of the last 12 months." });
                }

                var month = selectedMonth.Month;
                var year = selectedMonth.Year;
                // Sequential calls: the repositories may share the same scoped EF DbContext.
                var balances = accountRepo.GetAllAccountBalance(month, year)?.ToList() ?? [];
                var dailyPayments = payableRepo.GetAllAccountPayableByDay(today)?.ToList() ?? [];
                var fixedExpenses = fixedRepo.GetAllExpenseTypeTotal()?.OrderByDescending(x => x.EstimatedValue).ThenBy(x => x.Name).ToList() ?? [];
                // Preserve the original account argument (1) and excluded expense type (12).
                var monthlyExpenses = payableRepo.GetAllExpenseTotalByMonth(1, month, year)?
                    .Where(x => x.OID != 12).OrderByDescending(x => x.EstimatedValue).ThenBy(x => x.Name).ToList() ?? [];
                var totalTransfer = accountRepo.GetAccountTotalTransferToSaver(month, year);
                var marysBalance = accountRepo.GetAccountTotalTransferMaryPayment(month, year);

                var totals = balances.GroupBy(x => NormalizeCurrency(x.CurrencyType)).Select(group => new
                {
                    CurrencyType = group.Key,
                    TotalBalance = group.Where(x => x.OID != 6).Sum(x => x.Balance),
                    TotalRevenue = group.Where(x => x.OID != 5 && x.OID != 6).Sum(x => x.TotalRevenue)
                }).ToList();
                var months = Enumerable.Range(0, 12).Select(index => currentMonth.AddMonths(-index)).Select(date => new
                {
                    Value = date.ToString("M-yyyy", CultureInfo.InvariantCulture),
                    Label = date.ToString("MMMM / yyyy", CultureInfo.GetCultureInfo("en-AU"))
                }).ToList();

                return Ok(new
                {
                    AccountBalanceList = balances.Where(x => !x.Name.Contains("INACTIVE")),
                    AccountPayableOfTheDayList = dailyPayments,
                    ExpenseTotalList = fixedExpenses.Select((item, index) =>
                                                            {
                                                                item.OID = index + 1;
                                                                return item;
                                                            }),
                    ExpenseTotalByMonthList = monthlyExpenses,
                    mes_ano = selectedMonth.ToString("M-yyyy", CultureInfo.InvariantCulture),
                    TotalTransferToSaverAccount = totalTransfer,
                    TotalToSaverAccount = marysBalance,
                    Months = months,
                    Today = today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                    ExpenseCurrency = settings.DefaultCurrency,
                    FixedExpenseTotal = fixedExpenses.Sum(x => x.EstimatedValue),
                    MonthlyExpenseTotal = monthlyExpenses.Sum(x => x.EstimatedValue),
                    AccountTotals = totals,
                    OffsetTransferTarget = settings.OffsetTransferTarget,
                    OffsetTransferMissing = settings.OffsetTransferTarget is decimal target ? (decimal?)Math.Max(0m, target - totalTransfer) : null
                });
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Unable to load dashboard for {Month}", mes_ano);
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "Unable to load the dashboard. Please try again." });
            }
        }

        private string NormalizeCurrency(string? value)
        {
            var code = string.IsNullOrWhiteSpace(value) ? settings.DefaultCurrency : value.Trim().ToUpperInvariant();
            return code is "$" or "A$" or "AU$" ? "AUD" : code;
        }

        public sealed class DashboardSettings
        {
            public string TimeZoneId { get; set; } = "Australia/Sydney";
            public string DefaultCurrency { get; set; } = "AUD";
            public decimal? OffsetTransferTarget { get; set; }
        }
    }
}
