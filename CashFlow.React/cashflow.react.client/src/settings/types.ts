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
