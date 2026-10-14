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
  Paper,
  Skeleton,
  Stack,
  TextField,
  Typography,
} from "@mui/material";
import SaveIcon from "@mui/icons-material/SaveOutlined";
import { useAuth } from "../AuthContext";
import { loadSettings, SettingsError, writeSettings } from "./api";
import type { RevenueTypeFormData, RevenueTypeSaveInput } from "./types";

type Values = {
  oid: number;
  name: string;
};
type Props = { data: RevenueTypeFormData; returnUrl: string; id?: string };

function AccountForm({ data, returnUrl, id }: Props) {
  const { expire } = useAuth();
  const navigate = useNavigate();
  const [values, setValues] = useState<Values>({
    oid: data.item?.oid ?? 0,
    name: data.item?.name ?? "",
  });
  const [errors, setErrors] = useState<Record<string, string>>({});
  const [error, setError] = useState("");
  const [busy, setBusy] = useState(false);
  const [unknownOutcome, setUnknownOutcome] = useState(false);
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
    if (!values.name) nextErrors.name = "Revenue type name is required.";
    setErrors(nextErrors);
    setError("");
    if (Object.keys(nextErrors).length) return;
    const input: RevenueTypeSaveInput = {
      OID: values.oid,
      name: values.name
    };
    setBusy(true);
    try {
      const response = await writeSettings(
        id ? `/api/Settings/RevenueType/${id}` : "/api/Settings/RevenueType",
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
          : "Unable to save this revenue type.",
      );
      if (reason instanceof SettingsError) {
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
      <Stack spacing={3}>
        <TextField
          label="Revenue type name"
          required
          fullWidth
          value={values.name || ""}
          disabled={busy || unknownOutcome}
          onChange={(event) => update("name", event.target.value)}
          error={!!errors.name}
          helperText={errors.name}
        >
        </TextField>
        <Stack direction="row" sx={{ justifyContent: "flex-end", gap: 1.5 }}>
          <Button component={Link} to={returnUrl} disabled={busy}>
            {unknownOutcome ? "Check account list" : "Cancel"}
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

export default function AccountFormPage() {
  const { id } = useParams();
  const [params] = useSearchParams();
  const location = useLocation();
  const { expire } = useAuth();
  const account = params.get("account") ?? "";
  const [retry, setRetry] = useState(0);
  const requestKey = `${id ?? "new"}|${account}|${retry}`;
  const [result, setResult] = useState<{
    key: string;
    data: RevenueTypeFormData | null;
    error: string;
  } | null>(null);
  const loading = result?.key !== requestKey;
  const data = loading ? null : result?.data;
  const error = loading ? "" : result?.error;
  const returnUrl = `/settings/revenue${location.search}`;
  useEffect(() => {
    const controller = new AbortController();
    const path = id
      ? `/api/Settings/RevenueType/${encodeURIComponent(id)}`
      : `/api/Settings/RevenueType/Form${account ? `?accountOID=${encodeURIComponent(account)}` : ""}`;
    loadSettings<RevenueTypeFormData>(path, controller.signal)
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
  }, [id, account, requestKey, expire]);

  return (
    <Box>
      <Typography component="h1" variant="h4" sx={{ fontWeight: 700, mb: 3 }}>
        {id ? "Edit Revenue Type Settings" : "New Revenue Type Settings"}
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
        <AccountForm
          key={requestKey}
          data={data}
          id={id}
          returnUrl={returnUrl}
        />
      )}
    </Box>
  );
}
