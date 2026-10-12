using System.ComponentModel.DataAnnotations;

namespace CashFlow.React.Server.Utils
{
    public class _SaveRequest
    {
        public long AccountOID { get; set; }
        [Required] public string DueDate { get; set; } = string.Empty;
        public string? Description { get; set; }
        [Required] public decimal? AccountValue { get; set; }
        public bool IsPaid { get; set; }
    }

    public sealed class RevenueSaveRequest : _SaveRequest
    {
        public long RevenueTypeOID { get; set; }
    }

    public sealed class ExpenseSaveRequest : _SaveRequest
    {
        public long ExpenseTypeOID { get; set; }
    }


    public sealed class PaymentRequest { [Required] public bool? IsPaid { get; set; } }
    public sealed class LaunchRequest
    {
        public long AccountOID { get; set; }
        [Required] public string From { get; set; } = string.Empty;
        [Required] public string To { get; set; } = string.Empty;
    }
}
