import { useState } from "react";
import type { ComponentProps } from "react";
import {
  Alert,
  Box,
  Button,
  MenuItem,
  Paper,
  Stack,
  TextField,
} from "@mui/material";
import SearchIcon from "@mui/icons-material/Search";
import type { Account, Revenue, Filters } from "./types";

type Props = {
  initial: Filters;
  accounts: Account[];
  revenues: Revenue[];
  onApply: (filters: Filters) => void;
};
export default function ReceivableFilters({
  initial,
  accounts,
  revenues,
  onApply,
}: Props) {
  const [filters, setFilters] = useState(initial);
  const [error, setError] = useState("");
  const submit: NonNullable<ComponentProps<"form">["onSubmit"]> = (event) => {
    event.preventDefault();
    if (!filters.accountOID || !filters.from || !filters.to) {
      setError("Select an account and both dates.");
      return;
    }
    if (filters.from > filters.to) {
      setError("From date must be on or before To date.");
      return;
    }
    setError("");
    onApply(filters);
  };
  return (
    <Paper
      component="form"
      onSubmit={submit}
      variant="outlined"
      sx={{ p: 2.5 }}
    >
      {error && (
        <Alert severity="error" sx={{ mb: 2 }}>
          {error}
        </Alert>
      )}
      <Box
        sx={{
          display: "grid",
          gridTemplateColumns: {
            xs: "1fr",
            sm: "1fr 1fr",
            lg: "1.2fr 1.2fr 1fr 1fr auto",
          },
          gap: 2,
          alignItems: "start",
        }}
      >
        <TextField
          select
          label="Account"
          value={filters.accountOID || ""}
          required
          size="small"
          onChange={(event) =>
            setFilters({ ...filters, accountOID: Number(event.target.value) })
          }
        >
          {accounts.map((account) => (
            <MenuItem key={account.oid} value={account.oid}>
              {account.name}
            </MenuItem>
          ))}
        </TextField>
        <TextField
          select
          label="Revenue"
          value={filters.revenueTypeOID}
          size="small"
          onChange={(event) =>
            setFilters({
              ...filters,
              revenueTypeOID: Number(event.target.value),
            })
          }
        >
          <MenuItem value={0}>All expenses</MenuItem>
          {revenues.map((revenue) => (
            <MenuItem key={revenue.oid} value={revenue.oid}>
              {revenue.name}
            </MenuItem>
          ))}
        </TextField>
        <TextField
          label="From"
          type="date"
          value={filters.from}
          required
          size="small"
          slotProps={{ inputLabel: { shrink: true } }}
          onChange={(event) =>
            setFilters({ ...filters, from: event.target.value })
          }
        />
        <TextField
          label="To"
          type="date"
          value={filters.to}
          required
          size="small"
          slotProps={{ inputLabel: { shrink: true } }}
          onChange={(event) =>
            setFilters({ ...filters, to: event.target.value })
          }
        />
        <Stack>
          <Button
            type="submit"
            variant="contained"
            startIcon={<SearchIcon />}
            sx={{ minHeight: 40 }}
          >
            Search
          </Button>
        </Stack>
      </Box>
    </Paper>
  );
}
