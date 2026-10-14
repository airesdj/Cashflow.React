using CashFlow.Core.DTO;
using CashFlow.DataAccess.Repositories;
using CashFlow.React.Server.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CashFlow.React.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    [AutoValidateAntiforgeryToken]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public class AccountPayableController : ControllerBase
    {
        private readonly AccountPayableEntityRepository accountRepo;
        private readonly ExpenseEntityRepository typeRepo;
        private readonly FixedExpenseEntityRepository fixedRepo;
        private readonly AccountEntityRepository onlyaccountRepo;
        private readonly ILogger<AccountPayableController> logger;

        public AccountPayableController(AccountPayableEntityRepository accountRepo,
            ExpenseEntityRepository typeRepo, FixedExpenseEntityRepository fixedRepo,
            AccountEntityRepository onlyaccountRepo,
            ILogger<AccountPayableController> logger)
        {
            this.accountRepo = accountRepo;
            this.typeRepo = typeRepo;
            this.fixedRepo = fixedRepo;
            this.onlyaccountRepo = onlyaccountRepo;
            this.logger = logger;
        }

        private async Task<IActionResult> Execute(Func<Task<IActionResult>> action, bool write = false)
        {
            try { return await action(); }
            catch (Exception exception)
            {
                logger.LogError(exception, "Account payable request failed: {Method} {Path}", Request.Method, Request.Path);
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    message = write ? "The change could not be confirmed. Refresh the list before trying again." : "Unable to load account payable data. Please try again.",
                    outcomeUnknown = write
                });
            }
        }

        [HttpGet]
        public Task<IActionResult> List([FromQuery] long? accountOID, [FromQuery] long expenseTypeOID = 0,
            [FromQuery] string? from = null, [FromQuery] string? to = null) => Execute(async () =>
            {
                var defaults = await Common.GetFixedFilterValues(onlyaccountRepo);
                var accountId = accountOID ?? defaults.Item2;
                var fromDate = defaults.Item3.Date;
                var toDate = defaults.Item4.Date;
                if ((!string.IsNullOrWhiteSpace(from) && !Common.TryDate(from, out fromDate)) ||
                (!string.IsNullOrWhiteSpace(to) && !Common.TryDate(to, out toDate)))
                    return BadRequest(new { message = "Use valid dates in yyyy-MM-dd format." });
                if (fromDate > toDate)
                    return BadRequest(new { message = "From date must be on or before To date." });

                var accounts = await Common.GetAccountList(onlyaccountRepo);
                var expenses = await typeRepo.GetExpenseList();
                if (!accounts.Any(x => x.OID == accountId))
                    return BadRequest(new { message = "Select a valid account." });
                if (expenseTypeOID != 0 && !expenses.Any(x => x.OID == expenseTypeOID))
                    return BadRequest(new { message = "Select a valid expense type." });

                // Keep the original repository/helper argument types. Widen these casts if your repository supports larger IDs.
                var accountFilter = Convert.ToInt16(accountId);
                var expenseFilter = Convert.ToInt16(expenseTypeOID);
                var items = (await accountRepo.GetAccountList(accountFilter, expenseFilter, fromDate, toDate))
                                        .OrderByDescending(x => x.DueDate).ThenBy(x => x.OID).ToList();
                var balance = await Common.GetBalanceDetailsModel(onlyaccountRepo, accountFilter, expenseFilter, 0, fromDate, toDate);
                return Ok(new
                {
                    items,
                    accounts,
                    expenses,
                    balance,
                    filters = new { accountOID = accountId, expenseTypeOID, from = Common.Iso(fromDate), to = Common.Iso(toDate) },
                    today = Common.Iso(Common.LocalNow.Date)
                });
            });

        [HttpGet("Form")]
        public Task<IActionResult> Form([FromQuery] long? accountOID) => Execute(async () =>
        {
            var accounts = await Common.GetAccountList(onlyaccountRepo);
            var expenses = await typeRepo.GetExpenseList();
            var defaults = await Common.GetFixedFilterValues(onlyaccountRepo);
            var selectedAccount = accountOID ?? defaults.Item2;
            if (!accounts.Any(x => x.OID == selectedAccount)) selectedAccount = accounts.FirstOrDefault()?.OID ?? 0;
            var selectedExpense = expenses.Any(x => x.OID == 9) ? 9 : expenses.FirstOrDefault()?.OID ?? 0;
            return Ok(new
            {
                item = (AccountPayableDTO?)null,
                accounts,
                expenses,
                defaultAccountOID = selectedAccount,
                defaultExpenseTypeOID = selectedExpense,
                today = Common.Iso(Common.LocalNow.Date)
            });
        });

        [HttpGet("{id:int:min(1)}")]
        public Task<IActionResult> Get(int id) => Execute(async () =>
        {
            var item = await accountRepo.GetAccount(id);
            if (item is null) return NotFound(new { message = "This expense no longer exists." });
            return Ok(new
            {
                item,
                accounts = await Common.GetAccountList(onlyaccountRepo),
                expenses = await typeRepo.GetExpenseList(),
                defaultAccountOID = item.AccountOID,
                defaultExpenseTypeOID = item.ExpenseTypeOID,
                today = Common.Iso(Common.LocalNow.Date)
            });
        });

        [HttpPost]
        public Task<IActionResult> Create([FromBody] ExpenseSaveRequest input) => Execute(async () =>
        {
            var validation = await ValidateInput(input);
            var dueDate = validation.Item2;
            if (!validation.Item1) return ValidationProblem(ModelState);

            var now = Common.LocalNow;
            // Audit fields and Payday are controlled on the server, not supplied by the client.
            var model = new AccountPayableDTO
            {
                AccountOID = input.AccountOID,
                ExpenseTypeOID = input.ExpenseTypeOID,
                Description = input.Description ?? string.Empty,
                DueDate = dueDate,
                AccountValue = input.AccountValue!.Value,
                Payday = input.IsPaid ? now.Date : null,
                CreatedBy = User.Identity!.Name!,
                CreatedDate = now,
                UpdatedBy = string.Empty,
                UpdatedDate = null
            };

            var id = await accountRepo.Add(model);
            if (id <= 0) return StatusCode(500, new { message = "This expense was not added." });
            return CreatedAtAction(nameof(Get), new { id }, new { oid = id, message = "Expense added successfully." });
        }, write: true);

        [HttpPut("{id:int:min(1)}")]
        public Task<IActionResult> Edit(int id, [FromBody] ExpenseSaveRequest input) => Execute(async () =>
        {
            var validation = await ValidateInput(input);
            var dueDate = validation.Item2;
            if (!validation.Item1) return ValidationProblem(ModelState);

            var model = await accountRepo.GetAccount(id);
            if (model is null) return NotFound(new { message = "This expense no longer exists." });

            var now = Common.LocalNow;
            model.AccountOID = input.AccountOID;
            model.ExpenseTypeOID = input.ExpenseTypeOID;
            model.Description = input.Description ?? string.Empty;
            model.DueDate = dueDate;
            model.AccountValue = input.AccountValue!.Value;
            model.Payday = input.IsPaid ? model.Payday ?? now.Date : null;
            model.UpdatedBy = User.Identity!.Name!;
            model.UpdatedDate = now;

            if (!await accountRepo.Update(model)) return StatusCode(500, new { message = "This expense was not updated." });
            return Ok(new { message = "Expense updated successfully." });
        }, write: true);

        [HttpDelete("{id:int:min(1)}")]
        public Task<IActionResult> Delete(int id) => Execute(async () =>
        {
            if (await accountRepo.GetAccount(id) is null) return NotFound(new { message = "This expense no longer exists." });
            await accountRepo.Delete(id);
            return Ok(new { message = "Expense deleted successfully." });
        }, write: true);

        [HttpPut("{id:int:min(1)}/Payment")]
        public Task<IActionResult> Payment(int id, [FromBody] PaymentRequest input) => Execute(async () =>
        {
            var model = await accountRepo.GetAccount(id);
            if (model is null) return NotFound(new { message = "This expense no longer exists." });
            if (input.IsPaid is null) return BadRequest(new { message = "Specify whether the expense is paid." });

            var now = Common.LocalNow;
            // Set the requested state explicitly instead of toggling on each request.
            model.Payday = input.IsPaid.Value ? model.Payday ?? now.Date : null;
            model.UpdatedBy = User.Identity!.Name!;
            model.UpdatedDate = now;

            if (!await accountRepo.Update(model)) return StatusCode(500, new { message = "The payment status was not updated." });
            return Ok(new { message = input.IsPaid.Value ? "Expense marked as paid." : "Expense marked as unpaid." });
        }, write: true);


        [HttpPost("LaunchWeekly")]
        public Task<IActionResult> LaunchWeekly([FromBody] LaunchRequest input) => Execute(async () =>
        {
            if (!Common.TryDate(input.From, out var from) || !Common.TryDate(input.To, out var to) || from > to)
                return BadRequest(new { message = "Select a valid From/To period." });
            if ((to - from).TotalDays > 6)
                return BadRequest(new { message = "For weekly launching, select a period of no more than seven days." });
            if (!(await Common.GetAccountList(onlyaccountRepo)).Any(x => x.OID == input.AccountOID))
                return BadRequest(new { message = "Select a valid account." });

            var now = Common.LocalNow;
            var accountList = new List<AccountPayableDTO>();
            var runFortnightly = !await fixedRepo.IsFortnightlyLaunched(from);
            foreach (var item in await fixedRepo.GetFixedExpenseList())
            {
                if (item.IsMonthlyExpense && item.IsFortnightlyExpense)
                    return BadRequest(new { message = "A fixed expense is marked both monthly and fortnightly. Correct its schedule before launching." });

                DateTime? dueDate = null;
                if (!item.IsMonthlyExpense)
                {
                    if (item.IsFortnightlyExpense && !runFortnightly) continue;
                    // Preserve the existing AdditionalDays rule, relative to the period start.
                    dueDate = from.AddDays(item.AdditionalDays > 0 ? item.AdditionalDays : 0);
                }
                else
                {
                    if (item.DayOfMonth < 1 || item.DayOfMonth > 31)
                        return BadRequest(new { message = "A fixed expense has an invalid day of month. Correct its schedule before launching." });
                    // Clamp to the month's last day (including leap years and 30-day months).
                    for (var month = new DateTime(from.Year, from.Month, 1); month <= to; month = month.AddMonths(1))
                    {
                        var candidate = new DateTime(month.Year, month.Month,
                            Math.Min(item.DayOfMonth, DateTime.DaysInMonth(month.Year, month.Month)));
                        if (candidate >= from && candidate <= to) { dueDate = candidate; break; }
                    }
                    if (dueDate is null) continue;
                }
                accountList.Add(new AccountPayableDTO
                {
                    AccountOID = input.AccountOID,
                    ExpenseTypeOID = item.ExpenseTypeOID,
                    Description = item.Description,
                    AccountValue = item.ExpenseValue ?? 0,
                    CreatedBy = $"{User.Identity!.Name} - AutoLaunch",
                    CreatedDate = now,
                    DueDate = dueDate.Value,
                    Payday = null,
                    UpdatedBy = string.Empty,
                    UpdatedDate = null
                });
            }
            if (accountList.Count == 0) return Ok(new { scheduledCount = 0, message = "No fixed expenses are scheduled for this period." });
            // Existing bulk repository method. It must report persistence failures by throwing.
            await accountRepo.Add(accountList);
            return Ok(new { scheduledCount = accountList.Count, message = "Weekly expenses submitted successfully." });
        }, write: true);

        private async Task<(bool, DateTime)> ValidateInput(ExpenseSaveRequest input)
        {
            DateTime dueDate;
            if (!(await Common.GetAccountList(onlyaccountRepo)).Any(x => x.OID == input.AccountOID))
                ModelState.AddModelError(nameof(input.AccountOID), "Select a valid account.");
            if (!(await typeRepo.GetExpenseList()).Any(x => x.OID == input.ExpenseTypeOID))
                ModelState.AddModelError(nameof(input.ExpenseTypeOID), "Select a valid expense type.");
            if (!Common.TryDate(input.DueDate, out dueDate)) ModelState.AddModelError(nameof(input.DueDate), "Enter a valid due date.");
            if (input.AccountValue is null || input.AccountValue <= 0)
                ModelState.AddModelError(nameof(input.AccountValue), "Value must be greater than zero.");
            return (ModelState.IsValid, dueDate);
        }
    }
}
