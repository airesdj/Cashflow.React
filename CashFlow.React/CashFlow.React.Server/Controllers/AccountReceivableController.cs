using CashFlow.Core.DTO;
using CashFlow.DataAccess.Classes;
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
    public class AccountReceivableController : ControllerBase
    {
        private readonly AccountReceivableEntityRepository accountRepo;
        private readonly RevenueEntityRepository typeRepo;
        private readonly AccountEntityRepository onlyaccountRepo;
        private readonly ILogger<AccountReceivableController> logger;

        public AccountReceivableController(AccountReceivableEntityRepository accountRepo,
            RevenueEntityRepository typeRepo,
            AccountEntityRepository onlyaccountRepo,
            ILogger<AccountReceivableController> logger)
        {
            this.accountRepo = accountRepo;
            this.typeRepo = typeRepo;
            this.onlyaccountRepo = onlyaccountRepo;
            this.logger = logger;
        }

        private async Task<IActionResult> Execute(Func<Task<IActionResult>> action, bool write = false)
        {
            try { return await action(); }
            catch (Exception exception)
            {
                logger.LogError(exception, "Account receivable request failed: {Method} {Path}", Request.Method, Request.Path);
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    message = write ? "The change could not be confirmed. Refresh the list before trying again." : "Unable to load account receivable data. Please try again.",
                    outcomeUnknown = write
                });
            }
        }

        [HttpGet]
        public Task<IActionResult> List([FromQuery] long? accountOID, [FromQuery] long revenueTypeOID = 0,
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
                var revenues = await typeRepo.GetRevenueList();
                if (!accounts.Any(x => x.OID == accountId))
                    return BadRequest(new { message = "Select a valid account." });
                if (revenueTypeOID != 0 && !revenues.Any(x => x.OID == revenueTypeOID))
                    return BadRequest(new { message = "Select a valid revenue type." });

                // Keep the original repository/helper argument types. Widen these casts if your repository supports larger IDs.
                var accountFilter = Convert.ToInt16(accountId);
                var revenueFilter = Convert.ToInt16(revenueTypeOID);
                var items = (await accountRepo.GetAccountList(accountFilter, revenueFilter, fromDate, toDate))
                                            .OrderByDescending(x => x.DueDate).ThenBy(x => x.OID).ToList();
                var balance = await Common.GetBalanceDetailsModel(onlyaccountRepo, accountFilter, revenueFilter, 0, fromDate, toDate);
                return Ok(new
                {
                    items,
                    accounts,
                    revenues,
                    balance,
                    filters = new { accountOID = accountId, revenueTypeOID, from = Common.Iso(fromDate), to = Common.Iso(toDate) },
                    today = Common.Iso(Common.LocalNow.Date)
                });
            });


        [HttpGet("Form")]
        public Task<IActionResult> Form([FromQuery] long? accountOID) => Execute(async () =>
        {
            var accounts = await Common.GetAccountList(onlyaccountRepo);
            var revenues = await typeRepo.GetRevenueList();
            var defaults = await Common.GetFixedFilterValues(onlyaccountRepo);
            var selectedAccount = accountOID ?? defaults.Item2;
            if (!accounts.Any(x => x.OID == selectedAccount)) selectedAccount = accounts.FirstOrDefault()?.OID ?? 0;
            var selectedExpense = revenues.Any(x => x.OID == 9) ? 9 : revenues.FirstOrDefault()?.OID ?? 0;
            return Ok(new
            {
                item = (AccountPayableDTO?)null,
                accounts,
                revenues,
                defaultAccountOID = selectedAccount,
                defaultExpenseTypeOID = selectedExpense,
                today = Common.Iso(Common.LocalNow.Date)
            });
        });

        [HttpGet("{id:int:min(1)}")]
        public Task<IActionResult> Get(int id) => Execute(async () =>
        {
            var item = await accountRepo.GetAccount(id);
            if (item is null) return NotFound(new { message = "This revenue no longer exists." });
            return Ok(new
            {
                item,
                accounts = await Common.GetAccountList(onlyaccountRepo),
                revenues = await typeRepo.GetRevenueList(),
                defaultAccountOID = item.AccountOID,
                defaultRevenueTypeOID = item.RevenueTypeOID,
                today = Common.Iso(Common.LocalNow.Date)
            });
        });

        [HttpPost]
        public Task<IActionResult> Create([FromBody] RevenueSaveRequest input) => Execute(async () =>
        {
            var validation = await ValidateInput(input);
            var dueDate = validation.Item2;
            if (!validation.Item1) return ValidationProblem(ModelState);

            var now = Common.LocalNow;
            var model = new AccountReceivableDTO
            {
                AccountOID = input.AccountOID,
                RevenueTypeOID = input.RevenueTypeOID,
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
        public Task<IActionResult> Edit(int id, [FromBody] RevenueSaveRequest input) => Execute(async () =>
        {
            var validation = await ValidateInput(input);
            var dueDate = validation.Item2;
            if (!validation.Item1) return ValidationProblem(ModelState);

            var model = await accountRepo.GetAccount(id);
            if (model is null) return NotFound(new { message = "This expense no longer exists." });

            var now = Common.LocalNow;
            model.AccountOID = input.AccountOID;
            model.RevenueTypeOID = input.RevenueTypeOID;
            model.Description = input.Description ?? string.Empty;
            model.DueDate = dueDate;
            model.AccountValue = input.AccountValue!.Value;
            model.Payday = input.IsPaid ? model.Payday ?? now.Date : null;
            model.UpdatedBy = User.Identity!.Name!;
            model.UpdatedDate = now;

            if (!await accountRepo.Update(model)) return StatusCode(500, new { message = "This revenue was not updated." });
            return Ok(new { message = "Revenue updated successfully." });
        }, write: true);

        [HttpDelete("{id:int:min(1)}")]
        public Task<IActionResult> Delete(int id) => Execute(async () =>
        {
            if (await accountRepo.GetAccount(id) is null) return NotFound(new { message = "This revenue no longer exists." });
            await accountRepo.Delete(id);
            return Ok(new { message = "Revenue deleted successfully." });
        }, write: true);


        [HttpPut("{id:int:min(1)}/Payment")]
        public Task<IActionResult> Payment(int id, [FromBody] PaymentRequest input) => Execute(async () =>
        {
            var model = await accountRepo.GetAccount(id);
            if (model is null) return NotFound(new { message = "This revenue no longer exists." });
            if (input.IsPaid is null) return BadRequest(new { message = "Specify whether the revenue is paid." });

            var now = Common.LocalNow;
            model.Payday = input.IsPaid.Value ? model.Payday ?? now.Date : null;
            model.UpdatedBy = User.Identity!.Name!;
            model.UpdatedDate = now;

            if (!await accountRepo.Update(model)) return StatusCode(500, new { message = "The payment status was not updated." });
            return Ok(new { message = input.IsPaid.Value ? "Revenue marked as paid." : "Revenue marked as unpaid." });
        }, write: true);


        [HttpPut("LaunchMarysPayment")]
        public Task<IActionResult> LaunchMarysPayment([FromBody] LaunchPaymentRequest input) => Execute(async () =>
        {
            if (input.AccountOID == 0) return BadRequest(new { message = "Account invalid." });

            return await LaunchPayment(input.AccountOID,
                                 Common.RECEIVABLE_MARY_PAYMENT_OID,
                                 AppConfig.Settings_ReceivableMarysPaymentValue,
                                 "Marys");
        }, write: true);


        [HttpPut("LaunchAiresPayment")]
        public Task<IActionResult> LaunchAiresPayment([FromBody] LaunchPaymentRequest input) => Execute(async () =>
        {
            if (input.AccountOID == 0) return BadRequest(new { message = "Account invalid." });

            return await LaunchPayment(input.AccountOID,
                                 Common.RECEIVABLE_AIRES_PAYMENT_OID,
                                 AppConfig.Settings_ReceivableAiresPaymentValue,
                                 "Aires");
        }, write: true);


        private Task<IActionResult> LaunchPayment(long accountOID, int revenueTypeOID, decimal paymentValue, string type) => Execute(async () =>
        {
            var now = Common.LocalNow;

            var modelRequest = new AccountReceivableDTO()
            {
                DueDate = now.Date,
                Payday = now.Date,
                RevenueTypeOID = revenueTypeOID,
                AccountValue = paymentValue,
                Description = null!,
                AccountOID = accountOID,
                CreatedBy = User.Identity!.Name!,
                CreatedDate = now
            };

            if (await accountRepo.Add(modelRequest) == 0) return StatusCode(500, new { message = $"{type}' payment was not added." });
            return Ok(new { message = $"{type}' payment added successfully." });
        }, write: true);


        private async Task<(bool, DateTime)> ValidateInput(RevenueSaveRequest input)
        {
            DateTime dueDate;
            if (!(await Common.GetAccountList(onlyaccountRepo)).Any(x => x.OID == input.AccountOID))
                ModelState.AddModelError(nameof(input.AccountOID), "Select a valid account.");
            if (!(await typeRepo.GetRevenueList()).Any(x => x.OID == input.RevenueTypeOID))
                ModelState.AddModelError(nameof(input.RevenueTypeOID), "Select a valid revenue type.");
            if (!Common.TryDate(input.DueDate, out dueDate)) ModelState.AddModelError(nameof(input.DueDate), "Enter a valid due date.");
            if (input.AccountValue is null || input.AccountValue <= 0)
                ModelState.AddModelError(nameof(input.AccountValue), "Value must be greater than zero.");
            return (ModelState.IsValid, dueDate);
        }

        public class LaunchPaymentRequest
        {
            public long AccountOID { get; set; }
        }
    }
}
