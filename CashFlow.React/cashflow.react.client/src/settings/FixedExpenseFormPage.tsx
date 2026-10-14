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
  CircularProgress,
  InputAdornment,
  Paper,
  Skeleton,
  Stack,
  TextField,
  MenuItem,
  FormControlLabel,
  Checkbox,
  Typography,
} from "@mui/material";
import SaveIcon from "@mui/icons-material/SaveOutlined";
import { useAuth } from "../AuthContext";
import { currencyCode } from "../format";
import { loadSettings, SettingsError, writeSettings } from "./api";
import type { FixedExpenseFormData, FixedExpenseSaveInput } from "./types";

type Values = {
  oid: number;
  expenseTypeOID: number;
  description: string;
  isFortnightlyExpense: boolean;
  isMonthlyExpense: boolean;
  additionalDays: number;
  dayOfMonth: number;
  amount: string;
  isDeleted: boolean;
};
type Props = { data: FixedExpenseFormData; returnUrl: string; id?: string };

function FixedExpenseSettingsForm({ data, returnUrl, id }: Props) {
  const { expire } = useAuth();
  const navigate = useNavigate();
  const [values, setValues] = useState<Values>({
      oid: data.item?.oid ?? 0,
      expenseTypeOID: data.item?.expenseTypeOID ?? data.defaultExpenseTypeOID,
      description: data.item?.description ?? "",
      isFortnightlyExpense: data.item?.isFortnightlyExpense ?? false,
      isMonthlyExpense: data.item?.isMonthlyExpense ?? false,
      additionalDays: data.item?.additionalDays ?? 0,
      dayOfMonth: data.item?.dayOfMonth ?? 0,
      amount:
        data.item?.expenseValue != null
          ? String(data.item.expenseValue)
          : "",
      isDeleted: data.item?.isDeleted ?? false,
  });

  const [errors, setErrors] = useState<Record<string, string>>({});
  const [error, setError] = useState("");
  const [busy, setBusy] = useState(false);
  const [unknownOutcome, setUnknownOutcome] = useState(false);
  const update = <K extends keyof Values>(field: K, value: Values[K]) => {
    setValues((current) => ({ ...current, [field]: value }));
    setErrors((current) => ({ ...current, [field.toLowerCase()]: "" }));
  };
  const currency = currencyCode("AUD");
  const submit: NonNullable<ComponentProps<"form">["onSubmit"]> = async (
    event,
  ) => {
    event.preventDefault();
    if (busy || unknownOutcome) return;
    const nextErrors: Record<string, string> = {};
    if (!values.expenseTypeOID || values.expenseTypeOID == 0) nextErrors.expensetypeoid = "Expense type is required.";
    if (!values.description) nextErrors.description = "Description is required.";
    if (!values.amount) nextErrors.amount = "Value is required.";
    const amount = Number(values.amount);
    if (!values.amount.trim() || !Number.isFinite(amount) || amount <= 0)
      nextErrors.amount = "Value must be greater than 0.";
    setErrors(nextErrors);
    setError("");
    if (Object.keys(nextErrors).length) return;
    const input: FixedExpenseSaveInput = {
      OID: values.oid,
      expenseTypeOID: values.expenseTypeOID,
      description: values.description,
      isFortnightlyExpense: values.isFortnightlyExpense,
      isMonthlyExpense: values.isMonthlyExpense,
      additionalDays: values.additionalDays,
      dayOfMonth: values.dayOfMonth,
      expenseValue: amount,
      isDeleted: values.isDeleted,
    };

    setBusy(true);
    try {
      const response = await writeSettings(
        id ? `/api/Settings/FixedExpense/${id}` : "/api/Settings/FixedExpense",
        id ? "PUT" : "POST",
        input,
      );
      navigate(returnUrl, {
        replace: true,
        state: { message: response.message },
      });
    } catch (reason) {
      if (reason instanceof SettingsError && reason.status === 401) {
        expire();
        return;
      }
      setError(
        reason instanceof Error
          ? reason.message
          : "Unable to save this fixed expense.",
      );
      if (reason instanceof SettingsError) {
        setUnknownOutcome(reason.outcomeUnknown);
        const serverErrors: Record<string, string> = {};
        for (const [key, messages] of Object.entries(reason.errors)) {
          const normalized = (key.split(".").pop() ?? key).toLowerCase();
          serverErrors[normalized === "estimatedvalue" ? "amount" : normalized] =
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
      <Stack spacing={3}>
        <TextField
          select
          label="Expense Type"
          required
          value={values.expenseTypeOID || ""}
          disabled={busy || unknownOutcome}
          onChange={(event) => update("expenseTypeOID", Number(event.target.value))}
          error={!!errors.expensetypeoid}
          helperText={errors.expensetypeoid}
        >
          {data.expenseTypes.map((expense) => (
            <MenuItem key={expense.oid} value={expense.oid}>
              {expense.name}
            </MenuItem>
          ))}
        </TextField>
        <TextField
          label="Description"
          required
          fullWidth
          value={values.description || ""}
          disabled={busy || unknownOutcome}
          onChange={(event) => update("description", event.target.value)}
          error={!!errors.description}
          helperText={errors.description}
        >
        </TextField>

        <Box
          sx={{
            display: "grid",
            gridTemplateColumns: { xs: "1fr", sm: "1fr 1fr" },
            gap: 3,
          }}
        >
          <FormControlLabel
            control={
              <Checkbox
                checked={values.isMonthlyExpense}
                disabled={busy || unknownOutcome}
                onChange={(event) => update("isMonthlyExpense", event.target.checked)}
              />
            }
            label="Is Monthly?"
          />
          <TextField
            label="Day Of Month"
            required
            type="number"
            value={values.dayOfMonth}
            disabled={busy || unknownOutcome}
            onChange={(event) => update("dayOfMonth", Number(event.target.value))}
            error={!!errors.dayofmonth}
            helperText={errors.dayofmonth}
          />
        </Box>


        <Box
          sx={{
            display: "grid",
            gridTemplateColumns: { xs: "1fr", sm: "1fr 1fr" },
            gap: 3,
          }}
        >
          <TextField
            label="Additional Days"
            required
            type="number"
            value={values.additionalDays}
            disabled={busy || unknownOutcome}
            onChange={(event) => update("additionalDays", Number(event.target.value))}
            error={!!errors.additionaldays}
            helperText={errors.additionaldays}
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
              }
            }}
          />
        </Box>


          <FormControlLabel
            control={
              <Checkbox
                checked={values.isFortnightlyExpense}
                disabled={busy || unknownOutcome}
                onChange={(event) => update("isFortnightlyExpense", event.target.checked)}
              />
            }
            label="Is Fortnightly?"
          />


        <Stack direction="row" sx={{ justifyContent: "flex-end", gap: 1.5 }}>
          <Button component={Link} to={returnUrl} disabled={busy}>
            {unknownOutcome ? "Check fixed expense list" : "Cancel"}
          </Button>
          <Button
            type="submit"
            variant="contained"
            disabled={
              busy || unknownOutcome
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

export default function FixedExpenseSettingsFormPage() {
  const { id } = useParams();
  const [params] = useSearchParams();
  const location = useLocation();
  const { expire } = useAuth();
  const FixedExpense = params.get("FixedExpense") ?? "";
  const [retry, setRetry] = useState(0);
  const requestKey = `${id ?? "new"}|${FixedExpense}|${retry}`;
  const [result, setResult] = useState<{
    key: string;
    data: FixedExpenseFormData | null;
    error: string;
  } | null>(null);
  const loading = result?.key !== requestKey;
  const data = loading ? null : result?.data;
  const error = loading ? "" : result?.error;
  const returnUrl = `/settings/fixed-expense${location.search}`;
  useEffect(() => {
    const controller = new AbortController();
    const path = id
      ? `/api/Settings/FixedExpense/${encodeURIComponent(id)}`
      : `/api/Settings/FixedExpense/Form${FixedExpense ? `?FixedExpenseOID=${encodeURIComponent(FixedExpense)}` : ""}`;
    loadSettings<FixedExpenseFormData>(path, controller.signal)
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
              : "Unable to load the form.",
        });
      });
    return () => controller.abort();
  }, [id, FixedExpense, requestKey, expire]);

  return (
    <Box>
      <Typography component="h1" variant="h4" sx={{ fontWeight: 700, mb: 3 }}>
        {id ? "Edit Fixed Expense Settings" : "New Fixed Expense Settings"}
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
            Back to fixed expenses
          </Button>
        </>
      )}
      {!loading && data && (
        <FixedExpenseSettingsForm
          key={requestKey}
          data={data}
          id={id}
          returnUrl={returnUrl}
        />
      )}
    </Box>
  );
}
