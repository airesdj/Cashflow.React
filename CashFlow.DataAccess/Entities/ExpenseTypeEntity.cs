using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CashFlow.DataAccess.Entities
{
    [Table("expense_type")]
    public class ExpenseTypeEntity
    {
        [Key]
        public long OID { get; set; }
        public required string Name { get; set; }
        public decimal? EstimatedValue { get; set; }
    }
}
