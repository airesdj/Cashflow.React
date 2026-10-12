import { createContext, useCallback, useContext, useEffect, useState } from 'react';
import type { ReactNode } from 'react';
import { Alert, Box, Button, CircularProgress } from '@mui/material';
import { currentUser, signOut } from './auth';
import type { CurrentUser } from './auth';

type Auth = { user: CurrentUser | null; login: (user: CurrentUser) => void; logout: () => Promise<void>; loggingOut: boolean; expire: () => void; notice: string };
const AuthContext = createContext<Auth | null>(null);
export function useAuth(): Auth {
  const context = useContext(AuthContext);
  if (!context) throw new Error('AuthProvider is required.');
  return context;
}

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<CurrentUser | null>(null);
  const [loading, setLoading] = useState(true);
  const [loggingOut, setLoggingOut] = useState(false);
  const [error, setError] = useState('');
  const [notice, setNotice] = useState('');
  const [retry, setRetry] = useState(0);
  useEffect(() => {
    let active = true;
    currentUser().then((value) => { if (active) setUser(value); })
      .catch((reason: unknown) => { if (active) setError(reason instanceof Error ? reason.message : 'Unable to connect.'); })
      .finally(() => { if (active) setLoading(false); });
    return () => { active = false; };
  }, [retry]);

  const expire = useCallback(() => { setUser(null); setError(''); setNotice('Your session has expired. Please sign in again.'); }, []);

  const logout = async () => {
    if (loggingOut) return;
    setLoggingOut(true); setError('');
    try { await signOut(); setUser(null); setNotice(''); }
    catch (reason) { setError(reason instanceof Error ? reason.message : 'Unable to sign out.'); }
    finally { setLoggingOut(false); }
  };
  if (loading) return <Box sx={{ minHeight: '100dvh', display: 'grid', placeItems: 'center' }}><CircularProgress aria-label="Checking sign-in" /></Box>;
  // A failed startup check must not masquerade as a confirmed logged-out state.
  if (error && !user) return <Box sx={{ maxWidth: 500, mx: 'auto', p: 4, mt: 8 }}>
    <Alert severity="error">{error}</Alert><Button sx={{ mt: 2 }} onClick={() => { setLoading(true); setError(''); setRetry((value) => value + 1); }}>Retry connection</Button>
  </Box>;
  return <AuthContext.Provider value={{ user, login: (value) => { setUser(value); setNotice(''); setError(''); }, logout, loggingOut,
    expire, notice }}>
    {error && <Alert severity="error" onClose={() => setError('')} sx={{ position: 'fixed', bottom: 20, left: 20, right: 20, zIndex: 1600 }}>{error}</Alert>}
    {children}
  </AuthContext.Provider>;
}
