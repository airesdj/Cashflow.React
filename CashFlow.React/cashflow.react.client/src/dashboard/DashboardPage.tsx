import { useEffect, useState } from 'react';
import { useSearchParams } from 'react-router-dom';
import { Alert, Box, Button, Chip, FormControl, InputLabel, MenuItem, Select, Skeleton, Stack,
  Table, TableBody, TableCell, TableContainer, TableHead, TableRow, Typography } from '@mui/material';
import DashboardCard from '../components/DashboardCard';
import ExpenseList from '../components/ExpenseList';
import { useAuth } from '../AuthContext';
import { ApiError, getJson } from '../api';
import type { DashboardData } from './types';
import { currencyCode, dateLabel, money } from '../format';

const numberCell = { whiteSpace: 'nowrap', fontVariantNumeric: 'tabular-nums', fontSize: '.82rem' };
export default function DashboardPage() {
  const { expire } = useAuth();
  const [params, setParams] = useSearchParams();
  const month = params.get('month') ?? '';
  const [reload, setReload] = useState(0);
  const requestKey = `${month}:${reload}`;
  const [result, setResult] = useState<{ key: string; data: DashboardData | null; error: string } | null>(null);
  const loading = result?.key !== requestKey;
  const data = loading ? null : result?.data;
  const error = loading ? '' : result?.error;

  useEffect(() => {
    const controller = new AbortController();
    const query = month ? `?mes_ano=${encodeURIComponent(month)}` : '';
    getJson<DashboardData>(`/api/Dashboard${query}`, controller.signal)
      .then((value) => { if (!controller.signal.aborted) setResult({ key: requestKey, data: value, error: '' }); })
      .catch((reason: unknown) => {
        if (controller.signal.aborted) return;
        if (reason instanceof ApiError && reason.status === 401) { expire(); return; }
        setResult({ key: requestKey, data: null, error: reason instanceof Error ? reason.message : 'Unable to load the dashboard.' });
      });
    return () => controller.abort();
  }, [month, requestKey, expire]);

  const selectedLabel = data?.months.find((item) => item.value === data.mes_ano)?.label;
  const accountName = (oid: number) => data?.accountBalanceList.find((account) => account.oid === oid)?.name ?? `Account ${oid}`;
  const accountCurrency = (oid: number) => currencyCode(data?.accountBalanceList.find((account) => account.oid === oid)?.currencyType ?? '', data?.expenseCurrency);

return (
          <Box>
            <Stack
              direction="row"
              sx={{
                alignItems: 'center',
                justifyContent: 'space-between',
                gap: 2,
                mb: 3,
              }}
            >
              <Box>
                <Typography
                  component="h1"
                  variant="h4"
                  sx={{
                    fontWeight: 700,
                    fontSize: { xs: '1.75rem', sm: '2rem' },
                  }}
                >
                  Dashboard
                </Typography>

                <Typography color="text.secondary" sx={{ mt: 0.6 }}>
                  Your account balances and expenses at a glance.
                </Typography>
              </Box>
            </Stack>

            {error && (
              <Alert
                severity="error"
                sx={{ mb: 3 }}
                action={
                  <Button
                    color="inherit"
                    onClick={() => setReload((value) => value + 1)}
                  >
                    Retry
                  </Button>
                }
              >
                {error}
              </Alert>
            )}

            {error && month && (
              <Button sx={{ mb: 2 }} onClick={() => setParams({})}>
                Return to current month
              </Button>
            )}

            {loading && (
              <Box
                aria-label="Loading dashboard"
                aria-busy="true"
                sx={{
                  display: 'grid',
                  gridTemplateColumns: { xs: '1fr', xl: '1.2fr 1fr' },
                  gap: 3,
                }}
              >
                {[0, 1, 2, 3].map((item) => (
                  <Skeleton key={item} variant="rounded" height={280} />
                ))}
              </Box>
            )}

            {!loading && data && (
              <Box
                sx={{
                  display: 'grid',
                  gridTemplateColumns: {
                    xs: '1fr',
                    xl: 'minmax(0, 1.25fr) minmax(0, 1fr)',
                  },
                  gap: 3,
                  alignItems: 'start',
                }}
              >
                <Stack spacing={3} sx={{ minWidth: 0 }}>
                  <DashboardCard
                    title="Balance of All Accounts"
                    subtitle={`Revenue for ${selectedLabel}. Balance uses your existing account calculation.`}
                  >
                    <TableContainer sx={{ overflowX: 'auto' }}>
                      <Table
                        size="small"
                        aria-label="Balance of all accounts"
                        sx={{ minWidth: 690 }}
                      >
                        <TableHead>
                          <TableRow sx={{ bgcolor: '#f8fafc' }}>
                            <TableCell>Account</TableCell>
                            <TableCell align="right">
                              Aires
                              <br />
                              Payment
                            </TableCell>
                            <TableCell align="right">
                              Marys
                              <br />
                              Payment
                            </TableCell>
                            <TableCell align="right">
                              Total Revenue
                              <br />
                              (Monthly)
                            </TableCell>
                            <TableCell align="right">Current Balance</TableCell>
                          </TableRow>
                        </TableHead>

                        <TableBody>
                          {data.accountBalanceList.length === 0 && (
                            <TableRow>
                              <TableCell
                                colSpan={5}
                                sx={{ py: 4, color: 'text.secondary' }}
                              >
                                No accounts to display.
                              </TableCell>
                            </TableRow>
                          )}

                          {data.accountBalanceList.map((account) => {
                            const currency = currencyCode(
                              account.currencyType,
                              data.expenseCurrency,
                            );

                            return (
                              <TableRow key={account.oid} hover>
                                <TableCell
                                  sx={{
                                    fontSize: '.82rem',
                                    py: 1.8,
                                    minWidth: 160,
                                  }}
                                >
                                  {account.name}
                                </TableCell>

                                <TableCell align="right" sx={numberCell}>
                                  {money(account.totalRevenueAires, currency)}
                                </TableCell>

                                <TableCell align="right" sx={numberCell}>
                                  {money(account.totalRevenueMarys, currency)}
                                </TableCell>

                                <TableCell align="right" sx={numberCell}>
                                  {money(account.totalRevenue, currency)}
                                </TableCell>

                                <TableCell
                                  align="right"
                                  sx={{
                                    ...numberCell,
                                    fontWeight: 600,
                                    color:
                                      account.balance < 0
                                        ? 'error.main'
                                        : 'text.primary',
                                  }}
                                >
                                  {money(account.balance, currency)}
                                </TableCell>
                              </TableRow>
                            );
                          })}

                          {data.accountTotals.map((total) => (
                            <TableRow
                              key={total.currencyType}
                              sx={{
                                bgcolor: '#eef3f9',
                                '& td': { fontWeight: 700 },
                              }}
                            >
                              <TableCell></TableCell>

                              <TableCell align="right" sx={numberCell}>
                              </TableCell>

                              <TableCell align="right" sx={numberCell}>
                                T O T A L · {total.currencyType}
                              </TableCell>

                              <TableCell align="right" sx={numberCell}>
                                {money(total.totalRevenue, total.currencyType)}
                              </TableCell>

                              <TableCell align="right" sx={numberCell}>
                                {money(total.totalBalance, total.currencyType)}
                              </TableCell>
                            </TableRow>
                          ))}
                        </TableBody>
                      </Table>
                    </TableContainer>

                      <Typography
                        variant="caption"
                        color="text.secondary"
                        sx={{ display: 'block', p: 2 }}
                      >
                        Balance total excludes:{' Macquire account'}.
                      </Typography>

                  </DashboardCard>

                  <DashboardCard
                    title="Fixed Expenses"
                    subtitle="Recurring monthly totals"
                  >
                    <ExpenseList
                      items={data.expenseTotalList}
                      total={data.fixedExpenseTotal}
                      currency={data.expenseCurrency}
                    />
                  </DashboardCard>
                </Stack>

                <Stack spacing={3} sx={{ minWidth: 0 }}>
                  <DashboardCard
                    title="Pay List of the Day"
                    subtitle={`Due today · ${dateLabel(data.today)}`}
                    action={
                      <Chip
                        size="small"
                        label={`${
                          data.accountPayableOfTheDayList.filter(
                            (item) => !item.payday,
                          ).length
                        } unpaid`}
                        variant="outlined"
                      />
                    }
                  >
                    <TableContainer>
                      <Table
                        size="small"
                        aria-label="Payments due today"
                        sx={{ minWidth: 440 }}
                      >
                        <TableHead>
                          <TableRow sx={{ bgcolor: '#f8fafc' }}>
                            <TableCell>Account</TableCell>
                            <TableCell>Description</TableCell>
                            <TableCell align="right">Value</TableCell>
                            <TableCell>Paid?</TableCell>
                          </TableRow>
                        </TableHead>

                        <TableBody>
                          {data.accountPayableOfTheDayList.length === 0 && (
                            <TableRow>
                              <TableCell
                                colSpan={4}
                                sx={{ py: 4, color: 'text.secondary' }}
                              >
                                No payments due today.
                              </TableCell>
                            </TableRow>
                          )}

                          {data.accountPayableOfTheDayList.map((item) => (
                            <TableRow key={item.oid} hover>
                              <TableCell sx={{ fontSize: '.82rem', py: 1.8 }}>
                                {accountName(item.accountOID)}
                              </TableCell>

                              <TableCell sx={{ fontSize: '.82rem' }}>
                                {item.description}
                              </TableCell>

                              <TableCell align="right" sx={numberCell}>
                                {money(
                                  item.accountValue,
                                  accountCurrency(item.accountOID),
                                )}
                              </TableCell>

                              <TableCell>
                                <Chip
                                  label={item.payday ? 'Yes' : 'No'}
                                  size="small"
                                  color={item.payday ? 'success' : 'warning'}
                                  variant="outlined"
                                />
                              </TableCell>
                            </TableRow>
                          ))}
                        </TableBody>
                      </Table>
                    </TableContainer>
                  </DashboardCard>

                  <DashboardCard
                    title="Transfer to Offset Account"
                    subtitle={selectedLabel}
                  >
                    <Box sx={{ p: 2.5 }}>
                      <Typography variant="body2" color="text.secondary">
                        Transferred this month
                      </Typography>

                      <Typography
                        sx={{
                          fontWeight: 700,
                          fontSize: '1.65rem',
                          mt: 0.5,
                          fontVariantNumeric: 'tabular-nums',
                        }}
                      >
                        {money(
                          data.totalTransferToSaverAccount,
                          data.expenseCurrency,
                        )}
                      </Typography>

                        {data.totalTransferToSaverAccount < 1400 && (
                          <>
                            {' (Estimated: '}
                            {money(1400, data.expenseCurrency)}
                            {' / Missing: '}
                            {money(1400 - data.totalTransferToSaverAccount, data.expenseCurrency)}
                            {')'}
                          </>
                        )}

                      {data.offsetTransferTarget !== null && (
                        <Stack
                          direction="row"
                          sx={{ gap: 3, flexWrap: 'wrap', mt: 2 }}
                        >
                          <Box>
                            <Typography variant="caption" color="text.secondary">
                              Estimated
                            </Typography>

                            <Typography sx={{ fontWeight: 600 }}>
                              {money(
                                data.offsetTransferTarget,
                                data.expenseCurrency,
                              )}
                            </Typography>
                          </Box>

                          <Box>
                            <Typography variant="caption" color="text.secondary">
                              Still to transfer
                            </Typography>

                            <Typography sx={{ fontWeight: 600 }}>
                              {money(
                                data.offsetTransferMissing ?? 0,
                                data.expenseCurrency,
                              )}
                            </Typography>
                          </Box>
                        </Stack>
                      )}

                      <Stack
                        direction="row"
                        sx={{
                          mt: 2,
                          pt: 2,
                          borderTop: '1px solid #edf1f5',
                          justifyContent: 'space-between',
                          gap: 2,
                        }}
                      >
                        <Typography variant="body2" color="text.secondary">
                          Marys Payment Balance
                        </Typography>

                        <Typography variant="body2" sx={{ fontWeight: 600 }}>
                          {money(data.totalToSaverAccount, data.expenseCurrency)}
                        </Typography>
                      </Stack>
                    </Box>
                  </DashboardCard>

                  <DashboardCard
                    title="Total Expenses for Month"
                    subtitle="Choose a month to update the monthly dashboard figures"
                    action={
                      <FormControl size="small" sx={{ minWidth: 185 }}>
                        <InputLabel id="month-label">Last 12 months</InputLabel>

                        <Select
                          labelId="month-label"
                          label="Last 12 months"
                          value={data.mes_ano}
                          onChange={(event) => {
                            const next = new URLSearchParams(params);
                            next.set('month', event.target.value);
                            setParams(next);
                          }}
                        >
                          {data.months.map((item) => (
                            <MenuItem key={item.value} value={item.value}>
                              {item.label}
                            </MenuItem>
                          ))}
                        </Select>
                      </FormControl>
                    }
                  >
                    <ExpenseList
                      items={data.expenseTotalByMonthList}
                      total={data.monthlyExpenseTotal}
                      currency={data.expenseCurrency}
                    />
                  </DashboardCard>
                </Stack>
              </Box>
            )}
          </Box>
        );
}
