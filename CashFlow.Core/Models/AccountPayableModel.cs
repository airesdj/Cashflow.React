using CashFlow.Core.DTO;

namespace CashFlow.Core.Models
{
    public class AccountPayableModel : AccountPayableDTO
    {
        public bool isPaid { get; set; }

        public AccountPayableModel() { }

        public AccountPayableModel(AccountPayableDTO model)
        {
            OID = model.OID;
            AccountOID = model.AccountOID;
            DueDate = model.DueDate;
            Description = model.Description;
            ExpenseTypeOID = model.ExpenseTypeOID;
            Payday = model.Payday;
            AccountValue = model.AccountValue;
            CreatedBy = model.CreatedBy;
            CreatedDate = model.CreatedDate;
            UpdatedBy = model.UpdatedBy;
            UpdatedDate = model.UpdatedDate;
            isPaid = false;
        }
    }
}
