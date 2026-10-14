export type Account = { oid: number; name: string; currencyType: string };
export type AccountListData = {
  items: Account[];
  pageNumber: number;
  pageSize: number;
  totalItems: number;
  totalPages: number;
};
export type AccountFormData = {
  item: Account | null;
};
export type AccountSaveInput = {
  OID: number;
  name: string;
  currencyType: string;
};



export type ExpenseType = { oid: number; name: string; estimatedValue: number };
export type ExpenseTypeListData = {
  items: ExpenseType[];
  pageNumber: number;
  pageSize: number;
  totalItems: number;
  totalPages: number;
};
export type ExpenseTypeFormData = {
  item: ExpenseType | null;
};
export type ExpenseTypeSaveInput = {
  OID: number;
  name: string;
  estimatedValue: number;
};


export type RevenueType = { oid: number; name: string };
export type RevenueTypeListData = {
  items: ExpenseType[];
  pageNumber: number;
  pageSize: number;
  totalItems: number;
  totalPages: number;
};
export type RevenueTypeFormData = {
  item: RevenueType | null;
};
export type RevenueTypeSaveInput = {
  OID: number;
  name: string;
};




export type FixedExpense = { oid: number; expenseTypeOID: number; description: string; 
                             isFortnightlyExpense: boolean; isMonthlyExpense: boolean;
                             additionalDays: number; dayOfMonth: number;
                             expenseValue: number | null; isDeleted: boolean  };
export type FixedExpenseListData = {
  items: FixedExpense[];
  expenseTypes: ExpenseType[];
  pageNumber: number;
  pageSize: number;
  totalItems: number;
  totalPages: number;
};
export type FixedExpenseFormData = {
  item: FixedExpense | null;
  expenseTypes: ExpenseType[];
  defaultExpenseTypeOID: number;
};
export type FixedExpenseSaveInput = {
  OID: number;
  expenseTypeOID: number;
  description: string;
  isFortnightlyExpense: boolean;
  isMonthlyExpense: boolean;
  additionalDays: number;
  dayOfMonth: number;
  expenseValue: number | null;
  isDeleted: boolean;
};