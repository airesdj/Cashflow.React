export type Account = { oid: number; name: string; currencyType: string };
export type Expense = { oid: number; name: string; estimatedValue: number };
export type Cashflow = {
  accountOID: number;
  dueDate: string;
  description: string;
  payday: string | null;
  accountValue: number;
  launchType: string;
  currencyType: string;
};
export type Filters = {
  accountOID: number;
  excludeExpenseTypeOID: number;
  from: string;
  to: string;
};
export type Balance = {
  previousBalance: number;
  finalBalance: number;
  totalRevenues: number;
  totalExpenses: number;
};
export type ListData = {
  items: Cashflow[];
  pageNumber: number;
  pageSize: number;
  totalItems: number;
  totalPages: number;
  accounts: Account[];
  expenses: Expense[];
  balance: Balance;
  filters: Filters;
  today: string;
};

export function filterParams(filters: Filters): URLSearchParams {
  const params = new URLSearchParams({
    account: String(filters.accountOID),
    from: filters.from,
    to: filters.to,
  });
  if (filters.excludeExpenseTypeOID)
    params.set("expense", String(filters.excludeExpenseTypeOID));
  return params;
}

export function queryFromUrl(params: URLSearchParams): string {
  const query = new URLSearchParams();
  for (const [urlName, apiName] of [
    ["account", "accountOID"],
    ["expense", "excludeExpenseTypeOID"],
    ["from", "from"],
    ["to", "to"],
  ]) {
    const value = params.get(urlName);
    if (value !== null && value !== "") query.set(apiName, value);
  }
  return query.toString();
}
