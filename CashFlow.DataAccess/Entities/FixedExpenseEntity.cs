using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CashFlow.DataAccess.Entities
{
    [Table("fixed_expense")]
    public class FixedExpenseEntity
    {
        [Key]
        public long OID { get; set; }
        public required long ExpenseTypeOID { get; set; }
        public required string Description { get; set; }
        public required bool IsFortnightlyExpense { get; set; }
        public required bool IsMonthlyExpense { get; set; }
        public required byte AdditionalDays { get; set; }
        public required byte DayOfMonth { get; set; }
        public decimal? ExpenseValue { get; set; }
        public required bool IsDeleted { get; set; }
    }
}
