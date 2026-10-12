import { createTheme } from '@mui/material/styles';

export const theme = createTheme({
  palette: {
    primary: { main: '#08294c' },
    secondary: { main: '#ffcc00' },
    background: { default: '#f3f6fa', paper: '#ffffff' },
    text: { primary: '#152b42', secondary: '#627185' },
  },
  typography: { fontFamily: '"Segoe UI", Arial, sans-serif' },
  shape: { borderRadius: 12 },
  components: {
    MuiButton: { styleOverrides: { root: { textTransform: 'none', fontWeight: 600, borderRadius: 8 } } },
    MuiOutlinedInput: { styleOverrides: { root: { borderRadius: 8 } } },
  },
});
