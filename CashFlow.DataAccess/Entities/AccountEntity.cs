using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CashFlow.DataAccess.Entities
{
    [Table("account")]
    public class AccountEntity
    {
        [Key]
        public long OID { get; set; }
        public required string Name { get; set; }
        public string? CurrencyType { get; set; }
    }
}
