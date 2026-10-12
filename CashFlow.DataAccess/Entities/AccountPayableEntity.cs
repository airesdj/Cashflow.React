using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CashFlow.DataAccess.Entities
{
    [Table("account_payable")]
    public class AccountPayableEntity
    {
        [Key]
        public long OID { get; set; }
        public required long AccountOID { get; set; }
        public required DateTime DueDate { get; set; }
        public string? Description { get; set; }
        public required long ExpenseTypeOID { get; set; }
        public DateTime? Payday { get; set; }
        public required decimal AccountValue { get; set; }
        public required string CreatedBy { get; set; }
        public required DateTime CreatedDate { get; set; }
        public string? UpdatedBy { get; set; } = null!;
        public DateTime? UpdatedDate { get; set; }
    }
}
