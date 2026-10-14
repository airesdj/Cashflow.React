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
  Typography,
} from "@mui/material";
import SaveIcon from "@mui/icons-material/SaveOutlined";
import { useAuth } from "../AuthContext";
import { currencyCode } from "../format";
import { loadSettings, SettingsError, writeSettings } from "./api";
import type { ExpenseTypeFormData, ExpenseTypeSaveInput } from "./types";

type Values = {
  oid: number;
  name: string;
  amount: string;
};
type Props = { data: ExpenseTypeFormData; returnUrl: string; id?: string };

function ExpenseTypeSettingsForm({ data, returnUrl, id }: Props) {
  const { expire } = useAuth();
  const navigate = useNavigate();
  const [values, setValues] = useState<Values>({
    oid: data.item?.oid ?? 0,
    name: data.item?.name ?? "",
    amount: data.item ? String(data.item.estimatedValue) : ""
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
    if (!values.name) nextErrors.name = "Expense type name is required.";
    if (!values.amount) nextErrors.amount = "Estimated value is required.";
    const amount = Number(values.amount);
    if (!values.amount.trim() || !Number.isFinite(amount) || amount < 0)
      nextErrors.amount = "Estimated value must be positive.";
    setErrors(nextErrors);
    setError("");
    if (Object.keys(nextErrors).length) return;
    const input: ExpenseTypeSaveInput = {
      OID: values.oid,
      name: values.name,
      estimatedValue: amount,
    };
    setBusy(true);
    try {
      const response = await writeSettings(
        id ? `/api/Settings/ExpenseType/${id}` : "/api/Settings/ExpenseType",
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
          : "Unable to save this expense type.",
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
          label="ExpenseType name"
          required
          fullWidth
          value={values.name || ""}
          disabled={busy || unknownOutcome}
          onChange={(event) => update("name", event.target.value)}
          error={!!errors.name}
          helperText={errors.name}
        >
        </TextField>

          <TextField
            label="Estimated Value"
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

        <Stack direction="row" sx={{ justifyContent: "flex-end", gap: 1.5 }}>
          <Button component={Link} to={returnUrl} disabled={busy}>
            {unknownOutcome ? "Check expense type list" : "Cancel"}
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

export default function ExpenseTypeSettingsFormPage() {
  const { id } = useParams();
  const [params] = useSearchParams();
  const location = useLocation();
  const { expire } = useAuth();
  const ExpenseType = params.get("ExpenseType") ?? "";
  const [retry, setRetry] = useState(0);
  const requestKey = `${id ?? "new"}|${ExpenseType}|${retry}`;
  const [result, setResult] = useState<{
    key: string;
    data: ExpenseTypeFormData | null;
    error: string;
  } | null>(null);
  const loading = result?.key !== requestKey;
  const data = loading ? null : result?.data;
  const error = loading ? "" : result?.error;
  const returnUrl = `/settings/Expense${location.search}`;
  useEffect(() => {
    const controller = new AbortController();
    const path = id
      ? `/api/Settings/ExpenseType/${encodeURIComponent(id)}`
      : `/api/Settings/ExpenseType/Form${ExpenseType ? `?ExpenseTypeOID=${encodeURIComponent(ExpenseType)}` : ""}`;
    loadSettings<ExpenseTypeFormData>(path, controller.signal)
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
  }, [id, ExpenseType, requestKey, expire]);

  return (
    <Box>
      <Typography component="h1" variant="h4" sx={{ fontWeight: 700, mb: 3 }}>
        {id ? "Edit Expense Type Settings" : "New Expense Type Settings"}
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
            Back to expense types
          </Button>
        </>
      )}
      {!loading && data && (
        <ExpenseTypeSettingsForm
          key={requestKey}
          data={data}
          id={id}
          returnUrl={returnUrl}
        />
      )}
    </Box>
  );
}
