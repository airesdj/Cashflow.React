export type AccountBalance = {
  oid: number; name: string; currencyType: string; balance: number;
  totalRevenue: number; totalRevenueAires: number; totalRevenueMarys: number;
};
export type ExpenseTotal = { oid: number; name: string; estimatedValue: number };
export type Payable = {
  oid: number; accountOID: number; dueDate: string; description: string;
  expenseTypeOID: number; payday: string | null; accountValue: number;
};
export type AccountTotal = {
  currencyType: string; totalBalance: number; totalRevenue: number;
};
export type DashboardData = {
  accountBalanceList: AccountBalance[];
  accountPayableOfTheDayList: Payable[];
  expenseTotalList: ExpenseTotal[];
  expenseTotalByMonthList: ExpenseTotal[];
  mes_ano: string;
  totalTransferToSaverAccount: number;
  totalToSaverAccount: number;
  months: { value: string; label: string }[];
  today: string;
  expenseCurrency: string;
  fixedExpenseTotal: number;
  monthlyExpenseTotal: number;
  accountTotals: AccountTotal[];
  offsetTransferTarget: number | null;
  offsetTransferMissing: number | null;
};
