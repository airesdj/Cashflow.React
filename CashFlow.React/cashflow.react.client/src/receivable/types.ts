export type Account = { oid: number; name: string; currencyType: string };
export type Revenue = { oid: number; name: string; estimatedValue: number };
export type Receivable = {
  oid: number;
  accountOID: number;
  dueDate: string;
  description: string;
  revenueTypeOID: number;
  payday: string | null;
  accountValue: number;
};
export type Filters = {
  accountOID: number;
  revenueTypeOID: number;
  from: string;
  to: string;
};
export type Balance = {
  previousBalance: number;
  finalBalance: number;
  totalRevenues: number;
};
export type ListData = {
  items: Receivable[];
  accounts: Account[];
  revenues: Revenue[];
  balance: Balance;
  filters: Filters;
  today: string;
};
export type FormData = {
  item: Receivable | null;
  accounts: Account[];
  revenues: Revenue[];
  defaultAccountOID: number;
  defaultRevenueTypeOID: number;
  today: string;
};
export type SaveInput = {
  accountOID: number;
  revenueTypeOID: number;
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
  if (filters.revenueTypeOID)
    params.set("revenue", String(filters.revenueTypeOID));
  return params;
}

export function queryFromUrl(params: URLSearchParams): string {
  const query = new URLSearchParams();
  for (const [urlName, apiName] of [
    ["account", "accountOID"],
    ["revenue", "revenueTypeOID"],
    ["from", "from"],
    ["to", "to"],
  ]) {
    const value = params.get(urlName);
    if (value !== null && value !== "") query.set(apiName, value);
  }
  return query.toString();
}
