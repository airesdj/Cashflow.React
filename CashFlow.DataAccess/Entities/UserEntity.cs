using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CashFlow.DataAccess.Entities
{
    [Table("user")]
    public class UserEntity
    {
        [Key]
        public long OID { get; set; }
        public required string UserName { get; set; }
        public required string Password { get; set; }
        public required string UserType { get; set; }
        public required string FullName { get; set; }
        public required DateTime RegisteredDate { get; set; }
    }
}
