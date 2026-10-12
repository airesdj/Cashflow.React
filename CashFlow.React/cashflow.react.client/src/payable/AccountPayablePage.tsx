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
import AddIcon from "@mui/icons-material/Add";
import EditIcon from "@mui/icons-material/EditOutlined";
import DeleteIcon from "@mui/icons-material/DeleteOutlined";
import LaunchIcon from "@mui/icons-material/EventRepeat";
import { useAuth } from "../AuthContext";
import { currencyCode, dateLabel, money } from "../format";
import { loadPayable, PayableError, writePayable } from "./api";
import { filterParams, queryFromUrl } from "./types";
import type { ListData } from "./types";
import PayableFilters from "./PayableFilters";
import PayableActionDialog from "./PayableActionDialog";
import type { PayableAction } from "./PayableActionDialog";

export default function AccountPayablePage() {
  const { expire } = useAuth();
  const location = useLocation();
  const [params, setParams] = useSearchParams();
  const query = queryFromUrl(params);
  const [revision, setRevision] = useState(0);
  const requestKey = `${query}|${revision}`;
  const [result, setResult] = useState<{
    key: string;
    data: ListData | null;
    error: string;
  } | null>(null);
  const [notice, setNotice] = useState(
    (location.state as { message?: string } | null)?.message ?? "",
  );
  const [action, setAction] = useState<PayableAction | null>(null);
  const [busy, setBusy] = useState(false);
  const [actionError, setActionError] = useState("");
  const [unknownOutcome, setUnknownOutcome] = useState(false);
  const loading = result?.key !== requestKey;
  const data = loading ? null : result?.data;
  const error = loading ? "" : result?.error;

  useEffect(() => {
    const controller = new AbortController();
    loadPayable<ListData>(
      `/api/AccountPayable${query ? `?${query}` : ""}`,
      controller.signal,
    )
      .then((value) => {
        if (!controller.signal.aborted)
          setResult({ key: requestKey, data: value, error: "" });
      })
      .catch((reason: unknown) => {
        if (controller.signal.aborted) return;
        if (reason instanceof PayableError && reason.status === 401) {
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
  const openAction = (value: PayableAction) => {
    setActionError("");
    setUnknownOutcome(false);
    setAction(value);
  };
  const closeAction = () => {
    if (busy) return;
    setAction(null);
    if (unknownOutcome) setRevision((value) => value + 1);
  };
  const confirmAction = async () => {
    if (!action || !data || busy || unknownOutcome) return;
    setBusy(true);
    setActionError("");
    try {
      const response =
        action.type === "delete"
          ? await writePayable(
              `/api/AccountPayable/${action.item.oid}`,
              "DELETE",
            )
          : action.type === "payment"
            ? await writePayable(
                `/api/AccountPayable/${action.item.oid}/Payment`,
                "PUT",
                { isPaid: !action.item.payday },
              )
            : await writePayable("/api/AccountPayable/LaunchWeekly", "POST", {
                accountOID: data.filters.accountOID,
                from: data.filters.from,
                to: data.filters.to,
              });
      setNotice(response.message);
      setAction(null);
      setRevision((value) => value + 1);
    } catch (reason) {
      if (reason instanceof PayableError && reason.status === 401) {
        expire();
        return;
      }
      setActionError(
        reason instanceof Error
          ? reason.message
          : "Unable to complete the action.",
      );
      setUnknownOutcome(
        reason instanceof PayableError && reason.outcomeUnknown,
      );
    } finally {
      setBusy(false);
    }
  };

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
            Account Payable
          </Typography>
          <Typography color="text.secondary" sx={{ mt: 0.6 }}>
            Manage your expenses and payments for the selected period.
          </Typography>
        </Box>
        <Stack direction="row" sx={{ gap: 1.5, flexWrap: "wrap" }}>
          <Button
            component={Link}
            to={`/account-payable/new${returnQuery ? `?${returnQuery}` : ""}`}
            variant="contained"
            startIcon={<AddIcon />}
          >
            New Account Payable
          </Button>
          <Button
            variant="outlined"
            startIcon={<LaunchIcon />}
            disabled={!data || busy}
            onClick={() => openAction({ type: "launch" })}
          >
            Launch Expenses Weekly
          </Button>
        </Stack>
      </Stack>
      {notice && (
        <Alert severity="success" sx={{ mb: 2 }} onClose={() => setNotice("")}>
          {notice}
        </Alert>
      )}
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
          <PayableFilters
            key={requestKey}
            initial={data.filters}
            accounts={data.accounts}
            expenses={data.expenses}
            onApply={(filters) => {
              setNotice("");
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
                sm: "repeat(3, minmax(0, 1fr))",
              },
              gap: 2,
              my: 3,
            }}
          >
            {[
              ["Previous Balance", data.balance.previousBalance],
              ["Final Balance", data.balance.finalBalance],
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
            <Stack
              direction="row"
              sx={{
                p: 2.5,
                justifyContent: "space-between",
                alignItems: "center",
                gap: 2,
              }}
            >
              <Typography
                component="h2"
                variant="h6"
                sx={{ fontSize: "1.05rem", fontWeight: 700 }}
              >
                Expenses
              </Typography>
              <Chip
                size="small"
                variant="outlined"
                label={`${data.items.length} items`}
              />
            </Stack>
            <TableContainer sx={{ maxHeight: "42vh", overflowY: 'auto', overflowX: 'hidden' }}>
              <Table
                size="small"
                stickyHeader
                aria-label="Account payable expenses"
                sx={{ minWidth: 760 }}
              >
                <TableHead>
                  <TableRow>
                    <TableCell>Due Date</TableCell>
                    <TableCell>Description</TableCell>
                    <TableCell align="right">Value</TableCell>
                    <TableCell>Payday</TableCell>
                    <TableCell align="right">Actions</TableCell>
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
                  {data.items.map((item) => {
                    const expenseName = data.expenses.find(
                      (expense) => expense.oid === item.expenseTypeOID,
                    )?.name;
                    const description =
                      [expenseName, item.description]
                        .filter(Boolean)
                        .join(" – ") || "Expense";
                    return (
                      <TableRow key={item.oid} hover>
                        <TableCell sx={{ whiteSpace: "nowrap", py: 1.8 }}>
                          <Typography
                            variant="body2"
                            sx={{
                              color: item.payday
                                ? "text.primary"
                                : "error.main"
                            }}
                          >
                          {dateLabel(item.dueDate)}
                          </Typography>
                        </TableCell>
                        <TableCell sx={{ minWidth: 180 }}>
                          <Typography
                            variant="body2"
                            sx={{
                              color: item.payday
                                ? "text.primary"
                                : "error.main"
                            }}
                          >
                            {description}
                          </Typography>
                        </TableCell>
                        <TableCell
                          align="right"
                          sx={{
                              color: item.payday
                                ? "text.primary"
                                : "error.main",
                            whiteSpace: "nowrap",
                            fontVariantNumeric: "tabular-nums",
                            fontWeight: 600,
                          }}
                        >
                          {money(item.accountValue, currency)}
                        </TableCell>
                        <TableCell sx={{ whiteSpace: "nowrap" }}>
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
                        <TableCell align="right">
                          <Stack
                            direction="row"
                            sx={{ gap: 0.5, justifyContent: "flex-end" }}
                          >
                            <Button
                              size="small"
                              variant="outlined"
                              startIcon={<EditIcon />}
                              component={Link}
                              to={`/account-payable/${item.oid}/edit?${returnQuery}`}
                              disabled={busy}
                            >
                              Edit
                            </Button>
                            <Button
                              size="small"
                              variant="outlined"
                              color="error"
                              startIcon={<DeleteIcon />}
                              disabled={busy}
                              onClick={() =>
                                openAction({ type: "delete", item })
                              }
                            >
                              Delete
                            </Button>
                            <Button
                              size="small"
                              variant="outlined"
                              color={item.payday ? 'warning' : 'info'}
                              disabled={busy}
                              sx={{ minWidth: 104 }}
                              onClick={() =>
                                openAction({ type: "payment", item })
                              }
                            >
                              {item.payday ? "Mark unpaid" : "Pay"}
                            </Button>
                          </Stack>
                        </TableCell>
                      </TableRow>
                    );
                  })}
                </TableBody>
              </Table>
            </TableContainer>
          </Paper>
          <PayableActionDialog
            action={action}
            busy={busy}
            error={actionError}
            unknownOutcome={unknownOutcome}
            filters={data.filters}
            accountName={account?.name ?? "Selected account"}
            currency={currency}
            onClose={closeAction}
            onConfirm={() => {
              void confirmAction();
            }}
          />
        </>
      )}
    </Box>
  );
}
