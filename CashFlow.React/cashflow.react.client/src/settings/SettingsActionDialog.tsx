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

export type SettingsAction =
  { type: "delete" };
type Props = {
  action: SettingsAction | null;
  busy: boolean;
  error: string;
  unknownOutcome: boolean;
  name: string;
  onClose: () => void;
  onConfirm: () => void;
};
export default function SettingsActionDialog({
  action,
  name,
  busy,
  error,
  unknownOutcome,
  onClose,
  onConfirm,
}: Props) {
  if (!action) return null;
  const title =
    action.type === "delete"
      ? "Delete Record" : "";
  const buttonText =
    action.type === "delete"
      ? "Delete" : "";
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
          <>
            <Typography>{"Are you sure you want to delete this record?"}</Typography>
            <Typography sx={{ mt: 2, fontWeight: 600 }}>
              {name}
            </Typography>
          </>
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
