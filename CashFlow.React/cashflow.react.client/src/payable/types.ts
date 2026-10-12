export type Account = { oid: number; name: string; currencyType: string };
export type Expense = { oid: number; name: string; estimatedValue: number };
export type Payable = {
  oid: number;
  accountOID: number;
  dueDate: string;
  description: string;
  expenseTypeOID: number;
  payday: string | null;
  accountValue: number;
};
export type Filters = {
  accountOID: number;
  expenseTypeOID: number;
  from: string;
  to: string;
};
export type Balance = {
  previousBalance: number;
  finalBalance: number;
  totalExpenses: number;
};
export type ListData = {
  items: Payable[];
  accounts: Account[];
  expenses: Expense[];
  balance: Balance;
  filters: Filters;
  today: string;
};
export type FormData = {
  item: Payable | null;
  accounts: Account[];
  expenses: Expense[];
  defaultAccountOID: number;
  defaultExpenseTypeOID: number;
  today: string;
};
export type SaveInput = {
  accountOID: number;
  expenseTypeOID: number;
  dueDate: string;
  description: string;
  accountValue: number;
  isPaid: boolean;
};

export function filterParams(filters: Filters): URLSearchParams {
  const params = new URLSearchParams({
    account: String(filters.accountOID),
    from: filters.from,
    to: filters.to,
  });
  if (filters.expenseTypeOID)
    params.set("expense", String(filters.expenseTypeOID));
  return params;
}

export function queryFromUrl(params: URLSearchParams): string {
  const query = new URLSearchParams();
  for (const [urlName, apiName] of [
    ["account", "accountOID"],
    ["expense", "expenseTypeOID"],
    ["from", "from"],
    ["to", "to"],
  ]) {
    const value = params.get(urlName);
    if (value !== null && value !== "") query.set(apiName, value);
  }
  return query.toString();
}
