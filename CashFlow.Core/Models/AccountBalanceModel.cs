namespace CashFlow.Core.Models
{
    public class QueryResultModel
    {
        public decimal Total { get; set; }
    }

    public class AccountBalanceModel
    {
        public long OID { get; set; }
        public string Name { get; set; } = null!;
        public string CurrencyType { get; set; } = null!;
        public decimal Balance { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal TotalRevenueAires { get; set; }
        public decimal TotalRevenueMarys { get; set; }
    }
}
