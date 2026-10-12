import {
  Alert,
  Button,
  CircularProgress,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  Typography,
} from "@mui/material";
import type { Filters, Receivable } from "./types";
import { dateLabel, money } from "../format";

export type ReceivableAction =
  { type: "delete" | "payment"; item: Receivable } | { type: "maryPayment" } | { type: "airesPayment" };
type Props = {
  action: ReceivableAction | null;
  busy: boolean;
  error: string;
  unknownOutcome: boolean;
  filters: Filters;
  accountName: string;
  currency: string;
  onClose: () => void;
  onConfirm: () => void;
};
export default function ReceivableActionDialog({
  action,
  busy,
  error,
  unknownOutcome,
  filters,
  accountName,
  currency,
  onClose,
  onConfirm,
}: Props) {
  if (!action) return null;
  const title =
    action.type === "delete"
      ? "Delete Account Receivable"
      : action.type === "maryPayment"
        ? "Launch Mary Payment "
          : action.type === "airesPayment"
            ? "Launch Aires Payment "
                : action.item.payday
                  ? "Mark Account Receivable as Unpaid"
                  : "Pay Account Receivable";
  const buttonText =
    action.type === "delete"
      ? "Delete"
      : action.type === "maryPayment" ||
        action.type === "airesPayment"
        ? "Add payment"
            : action.item.payday
                ? "Mark unpaid"
                : "Pay";
  return (
    <Dialog
      open
      onClose={busy ? undefined : onClose}
      fullWidth
      maxWidth="sm"
      aria-labelledby="Receivable-dialog-title"
    >
      <DialogTitle id="Receivable-dialog-title">{title}</DialogTitle>
      <DialogContent>
        {error && (
          <Alert severity="error" sx={{ mb: 2 }}>
            {error}
          </Alert>
        )}
        {action.type === "maryPayment" || action.type === "airesPayment" ? (
          <>
            <Typography>
              Are you sure you wish to launch{" "}
              {action.type === "maryPayment" ? "Mary’s" : "Aires’s"} payment
              for today?
            </Typography>

            <Typography sx={{ mt: 2, fontWeight: 600 }}>
              {accountName}
            </Typography>

            <Typography variant="body2" color="text.secondary" sx={{ mt: 2 }}>
              This uses the configured payment amount for{" "}
              {action.type === "maryPayment" ? "Mary" : "Aires"}.
            </Typography>
          </>
        ) : (
            <>
            <Typography>
              {action.type === "delete"
                ? "Are you sure you want to delete this revenue?"
                : action.item.payday
                  ? "Cancel the payment and mark this revenue as unpaid?"
                  : "Mark this revenue as paid today?"}
            </Typography>
            <Typography sx={{ mt: 2, fontWeight: 600 }}>
              {action.item.description || "Revenue"}
            </Typography>
            <Typography color="text.secondary">
              Due {dateLabel(action.item.dueDate)} ·{" "}
              {money(action.item.accountValue, currency)}
            </Typography>
          </>
        )}
      </DialogContent>
      <DialogActions sx={{ px: 3, pb: 2 }}>
        <Button disabled={busy} onClick={onClose}>
          {unknownOutcome ? "Close and refresh" : "Cancel"}
        </Button>
        <Button
          variant="contained"
          color={action.type === "delete" ? "error" : "primary"}
          disabled={busy || unknownOutcome}
          onClick={onConfirm}
          startIcon={
            busy ? <CircularProgress size={16} color="inherit" /> : undefined
          }
        >
          {busy ? "Please wait…" : buttonText}
        </Button>
      </DialogActions>
    </Dialog>
  );
}
