using CashFlow.Core.DTO;

namespace CashFlow.Core.Models
{
    public class AccountReceivableModel : AccountReceivableDTO
    {
        public bool isPaid { get; set; }

        public AccountReceivableModel() { }

        public AccountReceivableModel(AccountReceivableDTO model)
        {
            OID = model.OID;
            AccountOID = model.AccountOID;
            DueDate = model.DueDate;
            Description = model.Description;
            RevenueTypeOID = model.RevenueTypeOID;
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
