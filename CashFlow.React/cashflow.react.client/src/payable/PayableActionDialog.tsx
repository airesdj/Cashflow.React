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
import type { Filters, Payable } from "./types";
import { dateLabel, money } from "../format";

export type PayableAction =
  { type: "delete" | "payment"; item: Payable } | { type: "launch" };
type Props = {
  action: PayableAction | null;
  busy: boolean;
  error: string;
  unknownOutcome: boolean;
  filters: Filters;
  accountName: string;
  currency: string;
  onClose: () => void;
  onConfirm: () => void;
};
export default function PayableActionDialog({
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
      ? "Delete Account Payable"
      : action.type === "launch"
        ? "Launch Expenses Weekly"
        : action.item.payday
          ? "Mark Account Payable as Unpaid"
          : "Pay Account Payable";
  const buttonText =
    action.type === "delete"
      ? "Delete"
      : action.type === "launch"
        ? "Launch expenses"
        : action.item.payday
          ? "Mark unpaid"
          : "Pay";
  return (
    <Dialog
      open
      onClose={busy ? undefined : onClose}
      fullWidth
      maxWidth="sm"
      aria-labelledby="payable-dialog-title"
    >
      <DialogTitle id="payable-dialog-title">{title}</DialogTitle>
      <DialogContent>
        {error && (
          <Alert severity="error" sx={{ mb: 2 }}>
            {error}
          </Alert>
        )}
        {action.type === "launch" ? (
          <>
            <Typography>
              Launch the fixed expenses scheduled for this period?
            </Typography>
            <Typography sx={{ mt: 2, fontWeight: 600 }}>
              {accountName}
            </Typography>
            <Typography color="text.secondary">
              {dateLabel(filters.from)} – {dateLabel(filters.to)}
            </Typography>
            <Typography variant="body2" color="text.secondary" sx={{ mt: 2 }}>
              This uses all applicable fixed expenses, even when the list is
              filtered to one expense type.
            </Typography>
          </>
        ) : (
          <>
            <Typography>
              {action.type === "delete"
                ? "Are you sure you want to delete this expense?"
                : action.item.payday
                  ? "Cancel the payment and mark this expense as unpaid?"
                  : "Mark this expense as paid today?"}
            </Typography>
            <Typography sx={{ mt: 2, fontWeight: 600 }}>
              {action.item.description || "Expense"}
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
