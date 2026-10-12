using CashFlow.Core.DTO;

namespace CashFlow.Core.Models.ViewModels
{
    public class DashboardViewModel
    {
        public List<AccountBalanceModel> AccountBalanceList { get; set; } = null!;
        public List<AccountPayableDTO> AccountPayableOfTheDayList { get; set; } = null!;
        public List<ExpenseTypeDTO> ExpenseTotalList { get; set; } = null!;
        public List<ExpenseTypeDTO> ExpenseTotalByMonthList { get; set; } = null!;
        public string mes_ano { get; set; } = null!;
        public decimal TotalTransferToSaverAccount { get; set; }
        public decimal TotalToSaverAccount { get; set; }
    }
}
