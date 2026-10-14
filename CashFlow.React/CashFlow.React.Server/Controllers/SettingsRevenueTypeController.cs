using CashFlow.Core.DTO;
using CashFlow.DataAccess.Repositories;
using CashFlow.React.Server.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using X.PagedList.Extensions;

namespace CashFlow.React.Server.Controllers
{
    [ApiController]
    [Route("api/Settings/RevenueType")]
    [Authorize]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public class SettingsRevenueTypeController : ControllerBase
    {
        private readonly RevenueEntityRepository revenueRepo;
        private readonly ILogger<SettingsRevenueTypeController> logger;

        public SettingsRevenueTypeController(RevenueEntityRepository revenueRepo,
                                         ILogger<SettingsRevenueTypeController> logger)
        {
            this.revenueRepo = revenueRepo;
            this.logger = logger;
        }

        private async Task<IActionResult> Execute(Func<Task<IActionResult>> action, bool write = false)
        {
            try { return await action(); }
            catch (Exception exception)
            {
                logger.LogError(exception, "Revenue type request failed: {Method} {Path}", Request.Method, Request.Path);
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    message = write ? "The change could not be confirmed. Refresh the list before trying again." : "Unable to load revenue type data. Please try again.",
                    outcomeUnknown = write
                });
            }
        }

        [HttpGet]
        public Task<IActionResult> List([FromQuery] int? page) => Execute(async () =>
        {
            var pagedItems = (await revenueRepo.GetRevenueList()).ToPagedList(page ?? 1, Common.TOTAL_ITEMS_PER_PAGE);

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
            var item = await revenueRepo.GetRevenue(id);
            if (item is null) return NotFound(new { message = "This revenue type no longer exists." });
            return Ok(new
            {
                item
            });
        });

        [HttpPost]
        public Task<IActionResult> Create([FromBody] RevenueTypeDTO input) => Execute(async () =>
        {
            if (!await ValidateInput(input)) return ValidationProblem(ModelState);

            var now = Common.LocalNow;
            var id = await revenueRepo.Add(input);

            if (id <= 0) return StatusCode(500, new { message = "This revenue type was not added." });
            return CreatedAtAction(nameof(Get), new { id }, new { oid = id, message = "Revenue type added successfully." });
        }, write: true);


        [HttpPut("{id:int:min(1)}")]
        public Task<IActionResult> Edit(int id, [FromBody] RevenueTypeDTO input) => Execute(async () =>
        {
            if (!await ValidateInput(input)) return ValidationProblem(ModelState);

            var model = await revenueRepo.GetRevenue(id);
            if (model is null) return NotFound(new { message = "This revenue type no longer exists." });

            if (!await revenueRepo.Update(input)) return StatusCode(500, new { message = "This revenue type was not updated." });
            return Ok(new { message = "Revenue type updated successfully." });
        }, write: true);


        [HttpDelete("{id:int:min(1)}")]
        public Task<IActionResult> Delete(int id) => Execute(async () =>
        {
            if (await revenueRepo.GetRevenue(id) is null) return NotFound(new { message = "This revenue type no longer exists." });
            await revenueRepo.Delete(id);
            return Ok(new { message = "Revenue type deleted successfully." });
        }, write: true);


        private async Task<bool> ValidateInput(RevenueTypeDTO input)
        {
            if (string.IsNullOrWhiteSpace(input.Name))
                ModelState.AddModelError(nameof(input.Name), "Revenue type name is required.");
            if (await revenueRepo.GetRevenueByName(input.Name, input.OID))
                ModelState.AddModelError(nameof(input.Name), "Revenue type name already exists.");
            return ModelState.IsValid;
        }
    }
}
