namespace CashFlow.Core.Models
{
    public class CashFlowModel : BalanceModel
    {
        public class AccountModel
        {
            public long AccountOID { get; set; }
            public string Description { get; set; } = null!;
            public DateTime DueDate { get; set; }
            public DateTime? Payday { get; set; }
            public string CurrencyType { get; set; } = null!;
            public decimal AccountValue { get; set; }
            public string LaunchType { get; set; } = null!;
        }
    }
}
