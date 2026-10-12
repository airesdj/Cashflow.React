import { useEffect, useState } from "react";
import { Link, useLocation, useSearchParams } from "react-router-dom";
import {
  Alert,
  Box,
  Button,
  Chip,
  Paper,
  Skeleton,
  Stack,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Typography,
} from "@mui/material";
import { useAuth } from "../AuthContext";
import { currencyCode, dateLabel, money } from "../format";
import { loadCashflow, CashflowError } from "./api";
import { filterParams, queryFromUrl } from "./types";
import type { ListData } from "./types";
import { Pagination } from "@mui/material";
import CashflowFilters from "./CashflowFilters";

export default function CashflowPage() {
  const { expire } = useAuth();
  const location = useLocation();
  const [params, setParams] = useSearchParams();
  const query = queryFromUrl(params);
  const [revision, setRevision] = useState(0);
  const [pageNumber, setPageNumber] = useState(1);
  const requestKey = `${query}|${pageNumber}|${revision}`;
  const [result, setResult] = useState<{
    key: string;
    data: ListData | null;
    error: string;
  } | null>(null);
  const loading = result?.key !== requestKey;
  const data = loading ? null : result?.data;
  const error = loading ? "" : result?.error;

  useEffect(() => {
    const requestParams = new URLSearchParams(query);
    requestParams.set("page", String(pageNumber));
    const controller = new AbortController();

    loadCashflow<ListData>(
      `/api/Cashflow?${requestParams.toString()}`,
      controller.signal,
    )
      .then((value) => {
        if (!controller.signal.aborted)
          setResult({ key: requestKey, data: value, error: "" });
      })
      .catch((reason: unknown) => {
        if (controller.signal.aborted) return;
        if (reason instanceof CashflowError && reason.status === 401) {
          expire();
          return;
        }
        setResult({
          key: requestKey,
          data: null,
          error:
            reason instanceof Error
              ? reason.message
              : "Unable to load expenses.",
        });
      });
    return () => controller.abort();
  }, [query, requestKey, expire]);

  const account = data?.accounts.find(
    (item) => item.oid === data.filters.accountOID,
  );
  const currency = currencyCode(account?.currencyType ?? "AUD");
  const returnQuery = data
    ? filterParams(data.filters).toString()
    : params.toString();

  return (
    <Box>
      <Stack
        direction="row"
        sx={{
          justifyContent: "space-between",
          alignItems: "center",
          gap: 2,
          flexWrap: "wrap",
          mb: 3,
        }}
      >
        <Box>
          <Typography component="h1" variant="h4" sx={{ fontWeight: 700 }}>
            Cash Flow
          </Typography>
          <Typography color="text.secondary" sx={{ mt: 0.6 }}>
            View your income, expenses and balance for the selected period.
          </Typography>
        </Box>
      </Stack>
      {error && (
        <Alert
          severity="error"
          sx={{ mb: 2 }}
          action={
            <Button
              color="inherit"
              onClick={() => setRevision((value) => value + 1)}
            >
              Retry
            </Button>
          }
        >
          {error}
        </Alert>
      )}
      {error && params.size > 0 && (
        <Button sx={{ mb: 2 }} onClick={() => setParams({})}>
          Reset filters
        </Button>
      )}
      {loading && (
        <Stack spacing={2} aria-busy="true">
          <Skeleton variant="rounded" height={110} />
          <Skeleton variant="rounded" height={100} />
          <Skeleton variant="rounded" height={350} />
        </Stack>
      )}
      {!loading && data && (
        <>
          <CashflowFilters
            key={requestKey}
            initial={data.filters}
            accounts={data.accounts}
            expenses={data.expenses}
            onApply={(filters) => {
              setPageNumber(1);
              const next = filterParams(filters);

              if (next.toString() === params.toString())
                setRevision((value) => value + 1);
              else setParams(next);
            }}
          />
          <Box
            sx={{
              display: "grid",
              gridTemplateColumns: {
                xs: "1fr",
                sm: "repeat(4, minmax(0, 1fr))",
              },
              gap: 2,
              my: 3,
            }}
          >
            {[
              ["Previous Balance", data.balance.previousBalance],
              ["Final Balance", data.balance.finalBalance],
              ["Total Revenues", data.balance.totalRevenues],
              ["Total Expenses", data.balance.totalExpenses],
            ].map(([label, amount]) => (
              <Paper key={String(label)} variant="outlined" sx={{ p: 2.5 }}>
                <Typography variant="body2" color="text.secondary">
                  {label}
                </Typography>
                <Typography
                  sx={{
                    fontSize: "1.5rem",
                    fontWeight: 700,
                    mt: 0.5,
                    fontVariantNumeric: "tabular-nums",
                  }}
                >
                  {money(Number(amount), currency)}
                </Typography>
              </Paper>
            ))}
          </Box>
          <Paper variant="outlined" sx={{ overflow: "hidden" }}>
            <TableContainer sx={{ maxHeight: "42vh", overflowY: 'auto', overflowX: 'hidden' }}>
              <Table
                size="small"
                stickyHeader
                aria-label="Cash flow"
                sx={{ minWidth: 760 }}
              >
                <TableHead>
                  <TableRow>
                    <TableCell>Due Date</TableCell>
                    <TableCell>Description</TableCell>
                    <TableCell align="right">Value</TableCell>
                    <TableCell>Payday</TableCell>
                  </TableRow>
                </TableHead>
                <TableBody>
                  {data.items.length === 0 && (
                    <TableRow>
                      <TableCell
                        colSpan={5}
                        sx={{
                          py: 5,
                          textAlign: "center",
                          color: "text.secondary",
                        }}
                      >
                        No expenses match these filters.
                      </TableCell>
                    </TableRow>
                  )}
                  {data.items.map((item, index) => {
                    const description = item.description || "Expense";
                    const rowColor = item.launchType == "EXPENSE" ? "error.main" : "#1976d2";
                    return (
                      <TableRow key={index} hover>
                        <TableCell sx={{ whiteSpace: "nowrap", py: 1.8 }}>
                          <Typography
                            variant="body2"
                            sx={{color: rowColor}}
                          >
                          {dateLabel(item.dueDate)}
                          </Typography>
                        </TableCell>
                        <TableCell sx={{ minWidth: 180 }}>
                          <Typography
                            variant="body2"
                            sx={{color: rowColor}}
                          >
                            {description}
                          </Typography>
                        </TableCell>
                        <TableCell
                          align="right"
                          sx={{
                            color: rowColor,
                            whiteSpace: "nowrap",
                            fontVariantNumeric: "tabular-nums",
                            fontWeight: 600,
                          }}
                        >
                          {money(item.accountValue, currency)}
                        </TableCell>
                        <TableCell sx={{ whiteSpace: "nowrap",
                                        color: rowColor}}>
                          {item.payday ? (
                            dateLabel(item.payday)
                          ) : (
                            <Chip
                              size="small"
                              label="Unpaid"
                              color="warning"
                              variant="outlined"
                            />
                          )}
                        </TableCell>
                      </TableRow>
                    );
                  })}
                </TableBody>
              </Table>
            </TableContainer>

            {data.totalPages > 1 && (
              <Box sx={{ display: "flex", justifyContent: "center", p: 2 }}>
                <Pagination
                  count={data.totalPages}
                  page={data.pageNumber}
                  onChange={(_, selectedPage) => setPageNumber(selectedPage)}
                  color="primary"
                  showFirstButton
                  showLastButton
                />
              </Box>
            )}
          </Paper>
        </>
      )}
    </Box>
  );
}
