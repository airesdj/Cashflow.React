namespace CashFlow.Core.Models
{
    public class BalanceModel
    {
        public decimal PreviousBalance { get; set; }
        public decimal FinalBalance { get; set; }
        public decimal TotalExpenses { get; set; }
        public decimal TotalRevenues { get; set; }
    }
}
