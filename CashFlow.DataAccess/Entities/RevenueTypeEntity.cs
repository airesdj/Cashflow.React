using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CashFlow.DataAccess.Entities
{
    [Table("revenue_type")]
    public class RevenueTypeEntity
    {
        [Key]
        public long OID { get; set; }
        public required string Name { get; set; }
    }
}
