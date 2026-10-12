using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CashFlow.DataAccess.Entities
{
    [Table("account_receivable")]
    public class AccountReceivableEntity
    {
        [Key]
        public long OID { get; set; }
        public required long AccountOID { get; set; }
        public required DateTime DueDate { get; set; }
        public string? Description { get; set; }
        public required long RevenueTypeOID { get; set; }
        public DateTime? Payday { get; set; }
        public decimal? AccountValue { get; set; }
        public required string CreatedBy { get; set; }
        public required DateTime CreatedDate { get; set; }
        public string? UpdatedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
    }
}
