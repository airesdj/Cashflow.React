import { Navigate, Route, Routes, useLocation } from 'react-router-dom';
import { Alert, Box, Paper, Typography } from '@mui/material';
import { AuthProvider, useAuth } from './AuthContext';
import LoginPage from './LoginPage';
import AppLayout from './components/AppLayout';
import DashboardPage from './dashboard/DashboardPage';
import AccountPayablePage from './payable/AccountPayablePage';
import AccountPayableFormPage from './payable/AccountPayableFormPage';
import AccountReceivablePage from './receivable/AccountReceivablePage';
import AccountReceivableFormPage from './receivable/AccountReceivableFormPage';
import CashflowPage from './cashflow/CashflowPage';

function LoginRoute() {
  const { user, login, notice } = useAuth();
  if (user) return <Navigate to="/dashboard" replace />;
  return <>{notice && <Alert severity="info">{notice}</Alert>}<LoginPage onSignedIn={login} /></>;
}
function ProtectedLayout() {
  const { user } = useAuth();
  return user ? <AppLayout /> : <Navigate to="/login" replace />;
}
function UpcomingPage({ title }: { title: string }) {
  return <Box><Typography component="h1" variant="h4" sx={{ fontWeight: 700, mb: 3 }}>{title}</Typography>
    <Paper variant="outlined" sx={{ p: 4 }}><Typography color="text.secondary">This page will be added in the next stage.</Typography></Paper></Box>;
}
function NotFound() {
  const { pathname } = useLocation();
  return <UpcomingPage title={`Page not found: ${pathname}`} />;
}
export default function App() {
  return <AuthProvider><Routes>
    <Route path="/login" element={<LoginRoute />} />
    <Route element={<ProtectedLayout />}>
      <Route index element={<Navigate to="/dashboard" replace />} />

      <Route path="dashboard" element={<DashboardPage />} />

      <Route path="account-payable" element={<AccountPayablePage />} />
      <Route path="account-payable/new" element={<AccountPayableFormPage />} />
      <Route path="account-payable/:id/edit" element={<AccountPayableFormPage />} />

      <Route path="account-receivable" element={<AccountReceivablePage />} />
      <Route path="account-receivable/new" element={<AccountReceivableFormPage />} />
      <Route path="account-receivable/:id/edit" element={<AccountReceivableFormPage />} />

      <Route path="cash-flow" element={<CashflowPage />} />

      <Route path="settings/account" element={<UpcomingPage title="Account" />} />
      <Route path="settings/expense" element={<UpcomingPage title="Expense" />} />
      <Route path="settings/revenue" element={<UpcomingPage title="Revenue" />} />
      <Route path="settings/fixed-expense" element={<UpcomingPage title="Fixed Expense" />} />

      <Route path="*" element={<NotFound />} />
    </Route>
  </Routes></AuthProvider>;
}
