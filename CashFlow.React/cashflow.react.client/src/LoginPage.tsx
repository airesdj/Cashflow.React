import { useState } from 'react';
import type { ComponentProps } from 'react';
import { Alert, Box, Button, CircularProgress, IconButton, InputAdornment, Paper, Stack, TextField, Typography } from '@mui/material';
import Visibility from '@mui/icons-material/Visibility';
import VisibilityOff from '@mui/icons-material/VisibilityOff';
import { signIn } from './auth';
import type { CurrentUser } from './auth';

type Props = { onSignedIn: (user: CurrentUser) => void };

export default function LoginPage({ onSignedIn }: Props) {
  const [userName, setUserName] = useState('');
  const [password, setPassword] = useState('');
  const [showPassword, setShowPassword] = useState(false);
  const [submitted, setSubmitted] = useState(false);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState('');
  const missingUser = submitted && !userName.trim();
  const missingPassword = submitted && !password;

  const handleSubmit: NonNullable<ComponentProps<'form'>['onSubmit']> = async (event) => {
    event.preventDefault();
    if (busy) return;
    setSubmitted(true);
    setError('');
    if (!userName.trim() || !password) return;
    setBusy(true);
    try { onSignedIn(await signIn(userName.trim(), password)); }
    catch (reason) { setError(reason instanceof Error ? reason.message : 'Unable to sign in. Please try again.'); }
    finally { setBusy(false); }
  };


return (
          <Box
            component="main"
            sx={{
              minHeight: '100dvh',
              display: 'grid',
              placeItems: 'center',
              p: 3,
              background:
                'radial-gradient(ellipse at top left, #e3edf7 0%, #f3f6fa 60%)',
            }}
          >
            <Box sx={{ width: '100%', maxWidth: 420 }}>
              <Paper
                elevation={0}
                sx={{
                  p: { xs: 3, sm: 5 },
                  border: '1px solid #e0e7ef',
                  boxShadow: '0 18px 55px rgba(8, 41, 76, 0.08)',
                  borderTop: '4px solid #ffcc00',
                }}
              >
                <Stack spacing={3}>
                  <Box
                    component="img"
                    src="/logo.png"
                    alt="CashFlow"
                    sx={{
                      width: 230,
                      maxWidth: '100%',
                      height: 'auto',
                      alignSelf: 'center',
                    }}
                  />

                  <Box sx={{ textAlign: 'center' }}>
                    <Typography
                      component="h1"
                      variant="h5"
                      sx={{ fontWeight: 700 }}
                    >
                      Welcome back
                    </Typography>

                    <Typography color="text.secondary" sx={{ mt: 1 }}>
                      Sign in to manage your cash flow.
                    </Typography>
                  </Box>

                  {error && <Alert severity="error">{error}</Alert>}

                  <Box
                    component="form"
                    onSubmit={handleSubmit}
                    noValidate
                    aria-busy={busy}
                  >
                    <Stack spacing={2.5}>
                      <TextField
                        label="Username"
                        name="username"
                        autoComplete="username"
                        autoFocus
                        fullWidth
                        value={userName}
                        onChange={(event) => setUserName(event.target.value)}
                        required
                        disabled={busy}
                        error={!!missingUser}
                        helperText={
                          missingUser ? 'Username is required.' : undefined
                        }
                      />

                      <TextField
                        label="Password"
                        name="password"
                        autoComplete="current-password"
                        fullWidth
                        required
                        type={showPassword ? 'text' : 'password'}
                        value={password}
                        disabled={busy}
                        onChange={(event) => setPassword(event.target.value)}
                        error={!!missingPassword}
                        helperText={
                          missingPassword ? 'Password is required.' : undefined
                        }
                        slotProps={{
                          input: {
                            endAdornment: (
                              <InputAdornment position="end">
                                <IconButton
                                  edge="end"
                                  aria-label={
                                    showPassword ? 'Hide password' : 'Show password'
                                  }
                                  onClick={() =>
                                    setShowPassword((value) => !value)
                                  }
                                  disabled={busy}
                                >
                                  {showPassword ? (
                                    <VisibilityOff />
                                  ) : (
                                    <Visibility />
                                  )}
                                </IconButton>
                              </InputAdornment>
                            ),
                          },
                        }}
                      />

                      <Button
                        type="submit"
                        variant="contained"
                        size="large"
                        fullWidth
                        disabled={busy}
                        disableElevation
                        sx={{ minHeight: 48 }}
                      >
                        {busy ? (
                          <>
                            <CircularProgress
                              size={20}
                              color="inherit"
                              sx={{ mr: 1 }}
                            />
                            Signing in…
                          </>
                        ) : (
                          'Sign in'
                        )}
                      </Button>
                    </Stack>
                  </Box>
                </Stack>
              </Paper>

              <Typography
                variant="body2"
                color="text.secondary"
                sx={{ mt: 3, textAlign: 'center' }}
              >
                CashFlow · Your finances, in one place.
              </Typography>
            </Box>
          </Box>
        );
}
