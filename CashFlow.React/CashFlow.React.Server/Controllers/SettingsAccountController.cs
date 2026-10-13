using CashFlow.Core.DTO;
using CashFlow.DataAccess.Repositories;
using CashFlow.React.Server.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using X.PagedList.Extensions;

namespace CashFlow.React.Server.Controllers
{
    [ApiController]
    [Route("api/Settings/Account")]
    [Authorize]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public class SettingsAccountController : ControllerBase
    {
        private readonly AccountEntityRepository accountRepo;
        private readonly ILogger<SettingsAccountController> logger;

        public SettingsAccountController(AccountEntityRepository accountRepo,
                                         ILogger<SettingsAccountController> logger)
        {
            this.accountRepo = accountRepo;
            this.logger = logger;
        }

        private IActionResult Execute(Func<IActionResult> action, bool write = false)
        {
            try { return action(); }
            catch (Exception exception)
            {
                logger.LogError(exception, "Account request failed: {Method} {Path}", Request.Method, Request.Path);
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    message = write ? "The change could not be confirmed. Refresh the list before trying again." : "Unable to load account data. Please try again.",
                    outcomeUnknown = write
                });
            }
        }

        [HttpGet]
        public IActionResult List([FromQuery] int? page) => Execute(() =>
        {
            var pagedItems = accountRepo.GetAccountList().ToPagedList(page ?? 1, 10);

            return Ok(new
            {
                items = pagedItems.ToList(),
                pageNumber = pagedItems.PageNumber,
                pageSize = pagedItems.PageSize,
                totalItems = pagedItems.TotalItemCount,
                totalPages = pagedItems.PageCount
            });
        });

        [HttpGet("Form")]
        public IActionResult Form([FromQuery] long? accountOID) => Execute(() =>
        {
            return Ok(new
            {
                item = (AccountDTO?)null
            });
        });



        [HttpGet("{id:int:min(1)}")]
        public IActionResult Get(int id) => Execute(() =>
        {
            var item = accountRepo.GetAccount(id);
            if (item is null) return NotFound(new { message = "This account no longer exists." });
            return Ok(new
            {
                item
            });
        });

        [HttpPost]
        public IActionResult Create([FromBody] AccountDTO input) => Execute(() =>
        {
            if (!ValidateInput(input)) return ValidationProblem(ModelState);

            var now = Common.LocalNow;
            var id = accountRepo.Add(input);
            if (id <= 0) return StatusCode(500, new { message = "This account was not added." });
            return CreatedAtAction(nameof(Get), new { id }, new { oid = id, message = "Account added successfully." });
        }, write: true);


        [HttpPut("{id:int:min(1)}")]
        public IActionResult Edit(int id, [FromBody] AccountDTO input) => Execute(() =>
        {
            if (!ValidateInput(input)) return ValidationProblem(ModelState);
            var model = accountRepo.GetAccount(id);
            if (model is null) return NotFound(new { message = "This account no longer exists." });
            if (!accountRepo.Update(model)) return StatusCode(500, new { message = "This account was not updated." });
            return Ok(new { message = "Account updated successfully." });
        }, write: true);


        [HttpDelete("{id:int:min(1)}")]
        public IActionResult Delete(int id) => Execute(() =>
        {
            if (accountRepo.GetAccount(id) is null) return NotFound(new { message = "This account no longer exists." });
            accountRepo.Delete(id);
            return Ok(new { message = "Account deleted successfully." });
        }, write: true);


        private bool ValidateInput(AccountDTO input)
        {
            if (string.IsNullOrWhiteSpace(input.Name))
                ModelState.AddModelError(nameof(input.Name), "Account name is required.");
            if (accountRepo.GetAccountByName(input.Name, input.OID))
                ModelState.AddModelError(nameof(input.Name), "Account name already exists.");
            if (string.IsNullOrWhiteSpace(input.CurrencyType))
                ModelState.AddModelError(nameof(input.CurrencyType), "Currency type is required.");
            return ModelState.IsValid;
        }
    }
}
