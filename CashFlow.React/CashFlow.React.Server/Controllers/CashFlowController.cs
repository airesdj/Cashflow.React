using CashFlow.DataAccess.Repositories;
using CashFlow.React.Server.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using X.PagedList.Extensions;

namespace CashFlow.React.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public class CashFlowController : ControllerBase
    {
        private readonly AccountEntityRepository accountRepo;
        private readonly CashFlowRepository cashRepo;
        private readonly ExpenseEntityRepository typeRepo;
        private readonly ILogger<CashFlowController> logger;

        public CashFlowController(AccountEntityRepository accountRepo,
                                  CashFlowRepository cashRepo,
                                  ExpenseEntityRepository typeRepo,
                                  ILogger<CashFlowController> logger)
        {
            this.accountRepo = accountRepo;
            this.cashRepo = cashRepo;
            this.typeRepo = typeRepo;
            this.logger = logger;
        }

        private IActionResult Execute(Func<IActionResult> action, bool write = false)
        {
            try { return action(); }
            catch (Exception exception)
            {
                logger.LogError(exception, "Cash flow request failed: {Method} {Path}", Request.Method, Request.Path);
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    message = write ? "The change could not be confirmed. Refresh the list before trying again." : "Unable to load cash flow data. Please try again.",
                    outcomeUnknown = write
                });
            }
        }

        [HttpGet]
        public IActionResult List([FromQuery] long? accountOID, [FromQuery] int? page,
                            [FromQuery] long excludeExpenseTypeOID = 0,
                            [FromQuery] string? from = null, [FromQuery] string? to = null) => Execute(() =>
            {
                var defaults = Utils.Common.GetFixedFilterValues(accountRepo);
                var accountId = accountOID ?? defaults.Item2;
                var fromDate = defaults.Item3.Date;
                var toDate = defaults.Item4.Date;
                if ((!string.IsNullOrWhiteSpace(from) && !Common.TryDate(from, out fromDate)) ||
                (!string.IsNullOrWhiteSpace(to) && !Common.TryDate(to, out toDate)))
                    return BadRequest(new { message = "Use valid dates in yyyy-MM-dd format." });
                if (fromDate > toDate)
                    return BadRequest(new { message = "From date must be on or before To date." });

                var accounts = Common.GetAccountList(accountRepo);
                var expenses = typeRepo.GetExpenseList();
                if (!accounts.Any(x => x.OID == accountId))
                    return BadRequest(new { message = "Select a valid account." });
                if (excludeExpenseTypeOID != 0 && !expenses.Any(x => x.OID == excludeExpenseTypeOID))
                    return BadRequest(new { message = "Select a valid expense type." });

                // Keep the original repository/helper argument types. Widen these casts if your repository supports larger IDs.
                var accountFilter = Convert.ToInt16(accountId);
                var expenseFilter = Convert.ToInt16(excludeExpenseTypeOID);
                var list = cashRepo.Get(accountFilter, expenseFilter, fromDate, toDate);
                var items = list.Item1.OrderByDescending(x => x.DueDate).ThenBy(x => x.Payday).ToArray();
                var balance = Common.GetBalanceDetailsModel(accountRepo, accountFilter, expenseFilter, 0, fromDate, toDate);

                var pagedItems = items.ToPagedList(page ?? 1, Common.TOTAL_ITEMS_PER_PAGE);

                return Ok(new
                {
                    items = pagedItems.ToList(),
                    pageNumber = pagedItems.PageNumber,
                    pageSize = pagedItems.PageSize,
                    totalItems = pagedItems.TotalItemCount,
                    totalPages = pagedItems.PageCount,
                    accounts,
                    expenses,
                    balance,
                    filters = new { accountOID = accountId, excludeExpenseTypeOID, from = Common.Iso(fromDate), to = Common.Iso(toDate) },
                    today = Common.Iso(Common.LocalNow.Date)
                });
            });
    }
}
