using CashFlow.Core.DTO;
using CashFlow.DataAccess.Repositories;
using CashFlow.React.Server.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using X.PagedList.Extensions;

namespace CashFlow.React.Server.Controllers
{
    [ApiController]
    [Route("api/Settings/FixedExpense")]
    [Authorize]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public class SettingsFixedExpenseController : ControllerBase
    {
        private readonly FixedExpenseEntityRepository fixedRepo;
        private readonly ILogger<SettingsRevenueTypeController> logger;

        public SettingsFixedExpenseController(FixedExpenseEntityRepository fixedRepo,
                                         ILogger<SettingsRevenueTypeController> logger)
        {
            this.fixedRepo = fixedRepo;
            this.logger = logger;
        }

        private async Task<IActionResult> Execute(Func<Task<IActionResult>> action, bool write = false)
        {
            try { return await action(); }
            catch (Exception exception)
            {
                logger.LogError(exception, "Fixed expense request failed: {Method} {Path}", Request.Method, Request.Path);
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    message = write ? "The change could not be confirmed. Refresh the list before trying again." : "Unable to load fixed expense data. Please try again.",
                    outcomeUnknown = write
                });
            }
        }

        [HttpGet]
        public Task<IActionResult> List([FromQuery] int? page) => Execute(async () =>
        {
            var pagedItems = (await fixedRepo.GetFixedExpenseList()).ToPagedList(page ?? 1, Common.TOTAL_ITEMS_PER_PAGE);

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
        public IActionResult Form() => Ok(new
        {
            item = (AccountDTO?)null
        });


        [HttpGet("{id:int:min(1)}")]
        public Task<IActionResult> Get(int id) => Execute(async () =>
        {
            var item = await fixedRepo.GetFixedExpense(id);
            if (item is null) return NotFound(new { message = "This fixed expense no longer exists." });
            return Ok(new
            {
                item
            });
        });

        [HttpPost]
        public Task<IActionResult> Create([FromBody] FixedExpenseDTO input) => Execute(async () =>
        {
            if (!ValidateInput(input)) return ValidationProblem(ModelState);

            var now = Common.LocalNow;
            var id = await fixedRepo.Add(input);

            if (id <= 0) return StatusCode(500, new { message = "This fixed expense was not added." });
            return CreatedAtAction(nameof(Get), new { id }, new { oid = id, message = "Fixed expense added successfully." });
        }, write: true);


        [HttpPut("{id:int:min(1)}")]
        public Task<IActionResult> Edit(int id, [FromBody] FixedExpenseDTO input) => Execute(async () =>
        {
            if (!ValidateInput(input)) return ValidationProblem(ModelState);

            var model = fixedRepo.GetFixedExpense(id);
            if (model is null) return NotFound(new { message = "This fixed expense no longer exists." });

            if (!await fixedRepo.Update(input)) return StatusCode(500, new { message = "This fixed expense was not updated." });
            return Ok(new { message = "Fixed expense updated successfully." });
        }, write: true);


        [HttpDelete("{id:int:min(1)}")]
        public Task<IActionResult> Delete(int id) => Execute(async () =>
        {
            if (await fixedRepo.GetFixedExpense(id) is null) return NotFound(new { message = "This fixed expense no longer exists." });
            await fixedRepo.Delete(id);
            return Ok(new { message = "Fixed expense deleted successfully." });
        }, write: true);


        private bool ValidateInput(FixedExpenseDTO input)
        {
            if (input.ExpenseTypeOID == 0)
                ModelState.AddModelError(nameof(input.ExpenseTypeOID), "Expense type is required.");
            if (string.IsNullOrWhiteSpace(input.Description))
                ModelState.AddModelError(nameof(input.Description), "Description is required.");
            if (input.ExpenseValue == 0)
                ModelState.AddModelError(nameof(input.Description), "Value must be greater than 0.");
            return ModelState.IsValid;
        }
    }
}
