import { useEffect, useState } from "react";
import { Link, useLocation, useSearchParams } from "react-router-dom";
import {
  Alert,
  Box,
  Button,
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
import { useAuth } from "../AuthContext";
import { loadSettings, SettingsError, writeSettings } from "./api";
import type { FixedExpense, FixedExpenseListData } from "./types";
import { Pagination } from "@mui/material";
import SettingsActionDialog from "./SettingsActionDialog";
import { currencyCode, money } from "../format";

export default function FixedExpenseSettingsPage() {
  const { expire } = useAuth();
  const location = useLocation();
  const [params, setParams] = useSearchParams();
  const [revision, setRevision] = useState(0);
  const [pageNumber, setPageNumber] = useState(1);
  const currency = currencyCode("AUD");
  const requestKey = `${pageNumber}|${revision}`;
  const [selectedItem, setSelectedItem] = useState<FixedExpense | null>(null);
  const [result, setResult] = useState<{
    key: string;
    data: FixedExpenseListData | null;
    error: string;
  } | null>(null);
  const [notice, setNotice] = useState(
    (location.state as { message?: string } | null)?.message ?? "",
  );
  const [busy, setBusy] = useState(false);
  const [actionError, setActionError] = useState("");
  const [unknownOutcome, setUnknownOutcome] = useState(false);
  const loading = result?.key !== requestKey;
  const data = loading ? null : result?.data;
  const error = loading ? "" : result?.error;

  useEffect(() => {
    const requestParams = new URLSearchParams();
    requestParams.set("page", String(pageNumber));
    const controller = new AbortController();

    loadSettings<FixedExpenseListData>(
      `/api/Settings/FixedExpense?${requestParams.toString()}`,
      controller.signal,
    )
      .then((value) => {
        if (!controller.signal.aborted)
          setResult({ key: requestKey, data: value, error: "" });
      })
      .catch((reason: unknown) => {
        if (controller.signal.aborted) return;
        if (reason instanceof SettingsError && reason.status === 401) {
          expire();
          return;
        }
        setResult({
          key: requestKey,
          data: null,
          error:
            reason instanceof Error
              ? reason.message
              : "Unable to load fixed expense.",
        });
      });
    return () => controller.abort();
  }, [requestKey, expire]);

const closeAction = () => {
  if (busy) return;

  setSelectedItem(null);

  if (unknownOutcome) {
    setRevision((value) => value + 1);
  }
};
 const confirmAction = async () => {
    if (!selectedItem || busy || unknownOutcome) return;
    setBusy(true);
    setActionError("");
    try {
      if (!selectedItem) return;
      const response = await writeSettings(
              `/api/Settings/FixedExpense/${selectedItem.oid}`,
              "DELETE",
            );
      setNotice(response.message);
      setSelectedItem(null);
      setRevision((value) => value + 1);
    } catch (reason) {
      if (reason instanceof SettingsError && reason.status === 401) {
        expire();
        return;
      }
      setActionError(
        reason instanceof Error
          ? reason.message
          : "Unable to complete the action.",
      );
      setUnknownOutcome(
        reason instanceof SettingsError && reason.outcomeUnknown,
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
            Settings - Fixed Expenses
          </Typography>
          <Typography color="text.secondary" sx={{ mt: 0.6 }}>
            Manage your fixed expenses.
          </Typography>
        </Box>
        <Stack direction="row" sx={{ gap: 1.5, flexWrap: "wrap" }}>
          <Button
            component={Link}
            to={`/settings/fixed-expense/new`}
            variant="contained"
            startIcon={<AddIcon />}
          >
            New Fixed Expense
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
          <Paper variant="outlined" sx={{ overflow: "hidden" }}>
            <TableContainer sx={{ maxHeight: "70vh", overflowY: 'auto', overflowX: 'hidden' }}>
              <Table
                size="small"
                stickyHeader
                aria-label="Settings - Expense Types"
                sx={{ minWidth: 760 }}
              >
                <TableHead>
                  <TableRow>
                    <TableCell sx={{ width: "20%" }}>Expense Type</TableCell>
                    <TableCell sx={{ width: "35%" }}>Description</TableCell>
                    <TableCell sx={{ width: "10%" }}>Fortnightly</TableCell>
                    <TableCell sx={{ width: "5%" }}>Monthly</TableCell>
                    <TableCell sx={{ width: "5%" }}>Days</TableCell>
                    <TableCell sx={{ width: "10%" }}>Day of Month</TableCell>
                    <TableCell sx={{ width: "10%" }} align="right">Value</TableCell>
                    <TableCell sx={{ width: "15%" }} align="right">Actions</TableCell>
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
                        No fixed expenses found.
                      </TableCell>
                    </TableRow>
                  )}
                  {data.items.map((item) => {
                        const expenseTypeName = data.expenseTypes.find(
                        (type) => type.oid === item.expenseTypeOID
                        )?.name ?? "Unknown expense type";
                    return (
                      <TableRow key={item.oid} hover>
                        <TableCell sx={{ whiteSpace: "nowrap", py: 1.8 }}>
                          <Typography variant="body2">{expenseTypeName}</Typography>
                        </TableCell>
                        <TableCell sx={{ whiteSpace: "nowrap", py: 1.8 }}>
                          <Typography variant="body2">{item.description}</Typography>
                        </TableCell>
                        <TableCell sx={{ whiteSpace: "nowrap", py: 1.8 }}>
                          <Typography variant="body2">
                          {item.isFortnightlyExpense ? "Yes" : "No"}
                          </Typography>
                        </TableCell>
                        <TableCell sx={{ whiteSpace: "nowrap", py: 1.8 }}>
                          <Typography variant="body2">
                          {item.isMonthlyExpense ? "Yes" : "No"}
                          </Typography>
                        </TableCell>
                        <TableCell sx={{ whiteSpace: "nowrap", py: 1.8 }}>
                          <Typography variant="body2">{String(item.additionalDays).padStart(2, "0")}</Typography>
                        </TableCell>
                        <TableCell sx={{ whiteSpace: "nowrap", py: 1.8 }}>
                          <Typography variant="body2">{String(item.dayOfMonth).padStart(2, "0")}</Typography>
                        </TableCell>
                        <TableCell align="right" sx={{ minWidth: 180 }}>
                          <Typography variant="body2">
                            {money(item.expenseValue ?? 0, currency)}
                          </Typography>
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
                              to={`/settings/fixed-expense/${item.oid}/edit`}
                              disabled={busy}
                            >
                              Edit
                            </Button>

                            {!item.isDeleted && (
                            <Button
                              size="small"
                              variant="outlined"
                              color="error"
                              startIcon={<DeleteIcon />}
                              disabled={busy}
                              onClick={() => {
                                  setActionError("");
                                  setUnknownOutcome(false);
                                  setSelectedItem(item);
                              }}
                            >
                              Delete
                            </Button>
                           )}
                          </Stack>
                        </TableCell>
                      </TableRow>
                    );
                  })}
                </TableBody>
              </Table>
            </TableContainer>

            {data.totalPages > 1 && (
              <Box sx={{ display: "flex", alignItems: "center", justifyContent: "center", flexWrap: "wrap", gap: 2, p: 1 }}>
                  <Typography variant="body2" color="text.secondary">
                    Page {data.pageNumber} of {data.totalPages}
                  </Typography>
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
          <SettingsActionDialog
            action={selectedItem ? { type: "delete" } : null}
            busy={busy}
            error={actionError}
            unknownOutcome={unknownOutcome}
            name={selectedItem?.description ?? ""}
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
