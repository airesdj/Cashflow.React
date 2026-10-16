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
  { type: "backup" };
type Props = {
  open: boolean;
  busy: boolean;
  error: string;
  unknownOutcome: boolean;
  onClose: () => void;
  onConfirm: () => void;
};

export default function DatabaseBackupDialog(
{ 
  open,
  busy,
  error,
  unknownOutcome,
  onClose,
  onConfirm,
}: Props) {
  const title = "Database Backup"
  return (
    <Dialog
      open={open}
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
            <Typography>{"Are you sure you want to run the database backup process?"}</Typography>
          </>
      </DialogContent>
      <DialogActions sx={{ px: 3, pb: 2 }}>
        <Button disabled={busy} onClick={onClose}>
          {unknownOutcome ? "Close and refresh" : "Cancel"}
        </Button>
        <Button
          variant="contained"
          color={"primary"}
          disabled={busy || unknownOutcome}
          onClick={onConfirm}
          startIcon={
            busy ? <CircularProgress size={16} color="inherit" /> : undefined
          }
        >
          {busy ? "Please wait…" : "Run"}
        </Button>
      </DialogActions>
    </Dialog>
  );
}
