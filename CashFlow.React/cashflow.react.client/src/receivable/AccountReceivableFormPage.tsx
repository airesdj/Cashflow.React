import { useEffect, useState } from "react";
import type { ComponentProps } from "react";
import {
  Link,
  useLocation,
  useNavigate,
  useParams,
  useSearchParams,
} from "react-router-dom";
import {
  Alert,
  Box,
  Button,
  Checkbox,
  CircularProgress,
  FormControlLabel,
  InputAdornment,
  MenuItem,
  Paper,
  Skeleton,
  Stack,
  TextField,
  Typography,
} from "@mui/material";
import SaveIcon from "@mui/icons-material/SaveOutlined";
import { useAuth } from "../AuthContext";
import { currencyCode } from "../format";
import { loadReceivable, ReceivableError, writeReceivable } from "./api";
import type { FormData, SaveInput } from "./types";

type Values = {
  accountOID: number;
  revenueTypeOID: number;
  dueDate: string;
  description: string;
  amount: string;
  isPaid: boolean;
};
type Props = { data: FormData; returnUrl: string; id?: string };

function ReceivableForm({ data, returnUrl, id }: Props) {
  const { expire } = useAuth();
  const navigate = useNavigate();
  const [values, setValues] = useState<Values>({
    accountOID: data.item?.accountOID ?? data.defaultAccountOID,
    revenueTypeOID: data.item?.revenueTypeOID ?? data.defaultRevenueTypeOID,
    dueDate: data.item?.dueDate.slice(0, 10) ?? data.today,
    description: data.item?.description ?? "",
    amount: data.item ? String(data.item.accountValue) : "",
    isPaid: !!data.item?.payday,
  });
  const [errors, setErrors] = useState<Record<string, string>>({});
  const [error, setError] = useState("");
  const [busy, setBusy] = useState(false);
  const [unknownOutcome, setUnknownOutcome] = useState(false);
  const currency = currencyCode(
    data.accounts.find((account) => account.oid === values.accountOID)
      ?.currencyType ?? "AUD",
  );
  const update = <K extends keyof Values>(field: K, value: Values[K]) => {
    setValues((current) => ({ ...current, [field]: value }));
    setErrors((current) => ({ ...current, [field.toLowerCase()]: "" }));
  };
  const submit: NonNullable<ComponentProps<"form">["onSubmit"]> = async (
    event,
  ) => {
    event.preventDefault();
    if (busy || unknownOutcome) return;
    const nextErrors: Record<string, string> = {};
    if (!values.accountOID) nextErrors.accountoid = "Account is required.";
    if (!values.revenueTypeOID)
      nextErrors.expensetypeoid = "Expense type is required.";
    if (!values.dueDate) nextErrors.duedate = "Due date is required.";
    const amount = Number(values.amount);
    if (!values.amount.trim() || !Number.isFinite(amount) || amount <= 0)
      nextErrors.amount = "Value must be greater than zero.";
    setErrors(nextErrors);
    setError("");
    if (Object.keys(nextErrors).length) return;
    const input: SaveInput = {
      accountOID: values.accountOID,
      revenueTypeOID: values.revenueTypeOID,
      dueDate: values.dueDate,
      description: values.description,
      accountValue: amount,
      isPaid: values.isPaid,
    };
    setBusy(true);
    try {
      const response = await writeReceivable(
        id ? `/api/AccountReceivable/${id}` : "/api/AccountReceivable",
        id ? "PUT" : "POST",
        input,
      );
      navigate(returnUrl, {
        replace: true,
        state: { message: response.message },
      });
    } catch (reason) {
      if (reason instanceof ReceivableError && reason.status === 401) {
        expire();
        return;
      }
      setError(
        reason instanceof Error
          ? reason.message
          : "Unable to save this expense.",
      );
      if (reason instanceof ReceivableError) {
        setUnknownOutcome(reason.outcomeUnknown);
        const serverErrors: Record<string, string> = {};
        for (const [key, messages] of Object.entries(reason.errors)) {
          const normalized = (key.split(".").pop() ?? key).toLowerCase();
          serverErrors[normalized === "accountvalue" ? "amount" : normalized] =
            messages.join(" ");
        }
        setErrors(serverErrors);
      }
    } finally {
      setBusy(false);
    }
  };

  return (
    <Paper
      component="form"
      onSubmit={submit}
      noValidate
      variant="outlined"
      aria-busy={busy}
      sx={{ p: { xs: 2.5, sm: 4 }, maxWidth: 800 }}
    >
      {error && (
        <Alert severity="error" sx={{ mb: 3 }}>
          {error}
        </Alert>
      )}
      {data.accounts.length === 0 && (
        <Alert severity="warning" sx={{ mb: 3 }}>
          Add an account before creating an expense.
        </Alert>
      )}
      <Stack spacing={3}>
        <TextField
          select
          label="Account"
          required
          value={values.accountOID || ""}
          disabled={busy || unknownOutcome}
          onChange={(event) => update("accountOID", Number(event.target.value))}
          error={!!errors.accountoid}
          helperText={errors.accountoid}
        >
          {data.accounts.map((account) => (
            <MenuItem key={account.oid} value={account.oid}>
              {account.name}
            </MenuItem>
          ))}
        </TextField>
        <TextField
          label="Description"
          value={values.description}
          disabled={busy || unknownOutcome}
          fullWidth
          onChange={(event) => update("description", event.target.value)}
          error={!!errors.description}
          helperText={errors.description}
        />
        <TextField
          select
          label="Revenue Type"
          required
          value={values.revenueTypeOID || ""}
          disabled={busy || unknownOutcome}
          onChange={(event) =>
            update("revenueTypeOID", Number(event.target.value))
          }
          error={!!errors.expensetypeoid}
          helperText={errors.expensetypeoid}
        >
          {data.revenues.map((revenue) => (
            <MenuItem key={revenue.oid} value={revenue.oid}>
              {revenue.name}
            </MenuItem>
          ))}
        </TextField>
        <Box
          sx={{
            display: "grid",
            gridTemplateColumns: { xs: "1fr", sm: "1fr 1fr" },
            gap: 3,
          }}
        >
          <TextField
            label="Due Date"
            required
            type="date"
            value={values.dueDate}
            disabled={busy || unknownOutcome}
            onChange={(event) => update("dueDate", event.target.value)}
            error={!!errors.duedate}
            helperText={errors.duedate}
            slotProps={{ inputLabel: { shrink: true } }}
          />
          <TextField
            label="Value"
            required
            type="number"
            value={values.amount}
            disabled={busy || unknownOutcome}
            onChange={(event) => update("amount", event.target.value)}
            error={!!errors.amount}
            helperText={errors.amount}
            slotProps={{
              htmlInput: { min: 0.01, step: 0.01 },
              input: {
                startAdornment: (
                  <InputAdornment position="start">{currency}</InputAdornment>
                ),
              },
            }}
          />
        </Box>
        <Box>
          <FormControlLabel
            control={
              <Checkbox
                checked={values.isPaid}
                disabled={busy || unknownOutcome}
                onChange={(event) => update("isPaid", event.target.checked)}
              />
            }
            label="Paid"
          />
          <Typography variant="body2" color="text.secondary">
            {data.item?.payday && values.isPaid
              ? "The existing payment date will be kept."
              : values.isPaid
                ? "The payment date will be set to today."
                : "This expense is unpaid."}
          </Typography>
        </Box>
        <Stack direction="row" sx={{ justifyContent: "flex-end", gap: 1.5 }}>
          <Button component={Link} to={returnUrl} disabled={busy}>
            {unknownOutcome ? "Check expense list" : "Cancel"}
          </Button>
          <Button
            type="submit"
            variant="contained"
            disabled={
              busy ||
              unknownOutcome ||
              !data.accounts.length ||
              !data.revenues.length
            }
            startIcon={
              busy ? (
                <CircularProgress size={16} color="inherit" />
              ) : (
                <SaveIcon />
              )
            }
          >
            {busy ? "Saving…" : "Save"}
          </Button>
        </Stack>
      </Stack>
    </Paper>
  );
}

export default function AccountReceivableFormPage() {
  const { id } = useParams();
  const [params] = useSearchParams();
  const location = useLocation();
  const { expire } = useAuth();
  const account = params.get("account") ?? "";
  const [retry, setRetry] = useState(0);
  const requestKey = `${id ?? "new"}|${account}|${retry}`;
  const [result, setResult] = useState<{
    key: string;
    data: FormData | null;
    error: string;
  } | null>(null);
  const loading = result?.key !== requestKey;
  const data = loading ? null : result?.data;
  const error = loading ? "" : result?.error;
  const returnUrl = `/account-receivable${location.search}`;
  useEffect(() => {
    const controller = new AbortController();
    const path = id
      ? `/api/AccountReceivable/${encodeURIComponent(id)}`
      : `/api/AccountReceivable/Form${account ? `?accountOID=${encodeURIComponent(account)}` : ""}`;
    loadReceivable<FormData>(path, controller.signal)
      .then((value) => {
        if (!controller.signal.aborted)
          setResult({ key: requestKey, data: value, error: "" });
      })
      .catch((reason: unknown) => {
        if (controller.signal.aborted) return;
        if (reason instanceof ReceivableError && reason.status === 401) {
          expire();
          return;
        }
        setResult({
          key: requestKey,
          data: null,
          error:
            reason instanceof Error
              ? reason.message
              : "Unable to load the form.",
        });
      });
    return () => controller.abort();
  }, [id, account, requestKey, expire]);

  return (
    <Box>
      <Typography component="h1" variant="h4" sx={{ fontWeight: 700, mb: 3 }}>
        {id ? "Edit Account Receivable" : "New Account Receivable"}
      </Typography>
      {loading && (
        <Skeleton variant="rounded" height={480} sx={{ maxWidth: 800 }} />
      )}
      {error && (
        <>
          <Alert
            severity="error"
            sx={{ mb: 2 }}
            action={
              <Button
                color="inherit"
                onClick={() => setRetry((value) => value + 1)}
              >
                Retry
              </Button>
            }
          >
            {error}
          </Alert>
          <Button component={Link} to={returnUrl}>
            Back to expenses
          </Button>
        </>
      )}
      {!loading && data && (
        <ReceivableForm
          key={requestKey}
          data={data}
          id={id}
          returnUrl={returnUrl}
        />
      )}
    </Box>
  );
}
