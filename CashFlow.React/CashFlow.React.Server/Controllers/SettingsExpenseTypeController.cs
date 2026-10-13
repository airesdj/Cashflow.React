using CashFlow.Core.DTO;
using CashFlow.DataAccess.Repositories;
using CashFlow.React.Server.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using X.PagedList.Extensions;

namespace CashFlow.React.Server.Controllers
{
    [ApiController]
    [Route("api/Settings/ExpenseType")]
    [Authorize]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public class SettingsExpenseTypeController : ControllerBase
    {
        private readonly ExpenseEntityRepository expenseRepo;
        private readonly ILogger<SettingsExpenseTypeController> logger;

        public SettingsExpenseTypeController(ExpenseEntityRepository expenseRepo,
                                         ILogger<SettingsExpenseTypeController> logger)
        {
            this.expenseRepo = expenseRepo;
            this.logger = logger;
        }

        private IActionResult Execute(Func<IActionResult> action, bool write = false)
        {
            try { return action(); }
            catch (Exception exception)
            {
                logger.LogError(exception, "Expense type request failed: {Method} {Path}", Request.Method, Request.Path);
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    message = write ? "The change could not be confirmed. Refresh the list before trying again." : "Unable to load expense type data. Please try again.",
                    outcomeUnknown = write
                });
            }
        }

        [HttpGet]
        public IActionResult List([FromQuery] int? page) => Execute(() =>
        {
            var pagedItems = expenseRepo.GetExpenseList().ToPagedList(page ?? 1, Common.TOTAL_ITEMS_PER_PAGE);

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
        public IActionResult Form() => Execute(() =>
        {
            return Ok(new
            {
                item = (ExpenseTypeDTO?)null
            });
        });

        [HttpGet("{id:int:min(1)}")]
        public IActionResult Get(int id) => Execute(() =>
        {
            var item = expenseRepo.GetExpense(id);
            if (item is null) return NotFound(new { message = "This expense type no longer exists." });
            return Ok(new
            {
                item
            });
        });

        [HttpPost]
        public IActionResult Create([FromBody] ExpenseTypeDTO input) => Execute(() =>
        {
            if (!ValidateInput(input)) return ValidationProblem(ModelState);

            var now = Common.LocalNow;
            var id = expenseRepo.Add(input);

            if (id <= 0) return StatusCode(500, new { message = "This expense type was not added." });
            return CreatedAtAction(nameof(Get), new { id }, new { oid = id, message = "Expense type added successfully." });
        }, write: true);


        [HttpPut("{id:int:min(1)}")]
        public IActionResult Edit(int id, [FromBody] ExpenseTypeDTO input) => Execute(() =>
        {
            if (!ValidateInput(input)) return ValidationProblem(ModelState);

            var model = expenseRepo.GetExpense(id);
            if (model is null) return NotFound(new { message = "This expense type no longer exists." });

            if (!expenseRepo.Update(input)) return StatusCode(500, new { message = "This expense type was not updated." });
            return Ok(new { message = "Expense type updated successfully." });
        }, write: true);


        [HttpDelete("{id:int:min(1)}")]
        public IActionResult Delete(int id) => Execute(() =>
        {
            if (expenseRepo.GetExpense(id) is null) return NotFound(new { message = "This expense type no longer exists." });
            expenseRepo.Delete(id);
            return Ok(new { message = "Expense type deleted successfully." });
        }, write: true);


        private bool ValidateInput(ExpenseTypeDTO input)
        {
            if (string.IsNullOrWhiteSpace(input.Name))
                ModelState.AddModelError(nameof(input.Name), "Expense type name is required.");
            if (expenseRepo.GetExpenseByName(input.Name, input.OID))
                ModelState.AddModelError(nameof(input.Name), "Expense type name already exists.");
            if (input.EstimatedValue < 0)
                ModelState.AddModelError(nameof(input.EstimatedValue), "Value must be positive.");
            return ModelState.IsValid;
        }
    }
}
