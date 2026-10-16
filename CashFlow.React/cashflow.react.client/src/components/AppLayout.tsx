import { useState } from 'react';
import type { ReactNode } from 'react';
import { Link, Outlet, useLocation } from 'react-router-dom';
import { AppBar, Avatar, Box, Button, Collapse, Divider,
  Drawer, IconButton, List, ListItemButton, ListItemIcon, ListItemText, Menu, MenuItem, Toolbar } from '@mui/material';
import MenuIcon from '@mui/icons-material/Menu';
import DashboardIcon from '@mui/icons-material/DashboardOutlined';
import PayableIcon from '@mui/icons-material/ReceiptLongOutlined';
import ReceivableIcon from '@mui/icons-material/PaymentsOutlined';
import CashFlowIcon from '@mui/icons-material/AccountBalanceOutlined';
import SettingsIcon from '@mui/icons-material/SettingsOutlined';
import ExpandMoreIcon from '@mui/icons-material/ExpandMore';
import ExpandLessIcon from '@mui/icons-material/ExpandLess';
import LogoutIcon from '@mui/icons-material/Logout';
import BackupIcon from '@mui/icons-material/BackupOutlined';
import AccountIcon from '@mui/icons-material/AccountBalanceWalletOutlined';
import { useAuth } from '../AuthContext';
import BackupActionDialog from "../DatabaseBackupDialog";
import { getJson } from '../api';
import { Alert } from "@mui/material";

const drawerWidth = 238;
export default function AppLayout() {
  const { user, logout, loggingOut } = useAuth();
  const location = useLocation();
  const [mobileOpen, setMobileOpen] = useState(false);
  const [settingsOpen, setSettingsOpen] = useState(location.pathname.startsWith('/settings'));
  const [anchor, setAnchor] = useState<HTMLElement | null>(null);
  const [busy, setBusy] = useState(false);
  const [actionError, setActionError] = useState("");
  const [unknownOutcome, setUnknownOutcome] = useState(false);
  const [backupOpen, setBackupOpen] = useState(false);
  const [notice, setNotice] = useState("");
  const [pageError, setPageError] = useState("");

  const closeAction = () => {
      if (busy) return;

      setBackupOpen(false);
      setActionError("");
      setUnknownOutcome(false);
  };

 const confirmAction = async () => {
    if (busy || unknownOutcome) return;
    setBusy(true);
    setActionError("");
    try {
        const response = await getJson<{ message: string }>(
          "/api/Dashboard/DatabaseBackup",
        );

        if (response.message === "SUCCESS"){
            setBackupOpen(false);
            setNotice("Database backup completed successfully.");
            return;
        }

      setNotice(response.message);
    } catch (reason) {
      setActionError(
        reason instanceof Error
          ? reason.message
          : "Unable to complete the database backup.",
      );
      setUnknownOutcome(false);
    } finally {
      setBusy(false);
    }
  };


  const navItem = (label: string, path: string, icon?: ReactNode) => (
    <ListItemButton key={path} component={Link} to={path} selected={location.pathname === path || location.pathname.startsWith(`${path}/`)}
      aria-current={location.pathname === path || location.pathname.startsWith(`${path}/`) ? 'page' : undefined} onClick={() => setMobileOpen(false)}
      sx={{ mx: 1.5, my: .4, pl: icon ? 1.5 : 5.8, borderRadius: 2,
        '&.Mui-selected': { bgcolor: '#eaf0f8', color: 'primary.main' },
        '&.Mui-selected .MuiListItemIcon-root': { color: 'primary.main' } }}>
      {icon && <ListItemIcon sx={{ minWidth: 34, color: 'text.secondary' }}>{icon}</ListItemIcon>}
      <ListItemText primary={label} slotProps={{ primary: { sx: { fontSize: '.9rem', fontWeight: location.pathname === path || location.pathname.startsWith(`${path}/`) ? 600 : 400 } } }} />
    </ListItemButton>
  );
  const navigation = <Box>
    <List aria-label="Main menu" sx={{ pt: 0 }}>
      {navItem('Dashboard', '/dashboard', <DashboardIcon fontSize="small" />)}
      {navItem('Account Payable', '/account-payable', <PayableIcon fontSize="small" />)}
      {navItem('Account Receivable', '/account-receivable', <ReceivableIcon fontSize="small" />)}
      {navItem('Cash Flow', '/cash-flow', <CashFlowIcon fontSize="small" />)}
      <Divider sx={{ mx: 3, my: 2 }} />
      <ListItemButton onClick={() => setSettingsOpen((value) => !value)} aria-expanded={settingsOpen} aria-controls="settings-menu"
        sx={{ mx: 1.5, borderRadius: 2 }}>
        <ListItemIcon sx={{ minWidth: 34, color: 'text.secondary' }}><SettingsIcon fontSize="small" /></ListItemIcon>
        <ListItemText primary="Settings" slotProps={{ primary: { sx: { fontSize: '.9rem' } } }} />
        {settingsOpen ? <ExpandLessIcon fontSize="small" /> : <ExpandMoreIcon fontSize="small" />}
      </ListItemButton>
      <Collapse in={settingsOpen} timeout="auto" unmountOnExit id="settings-menu">
        <List disablePadding>
          {navItem('Account', '/settings/account')}{navItem('Expense', '/settings/expense')}
          {navItem('Revenue', '/settings/revenue')}{navItem('Fixed Expense', '/settings/fixed-expense')}
        </List>
      </Collapse>
    </List>
  </Box>;

  return <Box sx={{ display: 'flex', minHeight: '100dvh' }}>
    <AppBar position="fixed" elevation={0} sx={{ zIndex: (theme) => theme.zIndex.drawer + 1, borderBottom: '3px solid #ffcc00' }}>
      <Toolbar sx={{ gap: 2 }}>
        <IconButton color="inherit" aria-label="Open navigation" onClick={() => setMobileOpen(true)} sx={{ display: { md: 'none' } }}><MenuIcon /></IconButton>
        <Box component={Link} to="/dashboard" aria-label="CashFlow dashboard" sx={{ display: 'flex', alignItems: 'center', bgcolor: 'white', borderRadius: 1, px: 1, py: .4 }}>
          <Box component="img" src="/logo.png" alt="CashFlow" sx={{ width: { xs: 130, sm: 160 }, height: 'auto' }} />
        </Box>
        <Box sx={{ flex: 1 }} />
        <Button color="inherit" onClick={(event) => setAnchor(event.currentTarget)} aria-label="Current user menu"
          aria-controls={anchor ? 'user-menu' : undefined} aria-expanded={!!anchor} aria-haspopup="true"
          startIcon={<Avatar sx={{ width: 30, height: 30, bgcolor: '#ffffff20', fontSize: '.85rem' }}>{user?.userName.slice(0, 1).toUpperCase()}</Avatar>}
          endIcon={<ExpandMoreIcon />}>
          <Box component="span" sx={{ display: { xs: 'none', sm: 'inline' } }}>{user?.userName}</Box>
        </Button>
        <Menu id="user-menu" anchorEl={anchor} open={!!anchor} onClose={() => setAnchor(null)}>
          <MenuItem disabled><AccountIcon fontSize="small" sx={{ mr: 1.5 }} />{user?.userName}</MenuItem>
          <Divider />
          <MenuItem onClick={() => {
                              setActionError("");
                              setUnknownOutcome(false);
                              setBackupOpen(true);
                            }}><BackupIcon fontSize="small" sx={{ mr: 1.5 }} />Database Backup</MenuItem>
          <MenuItem disabled={loggingOut} onClick={() => { setAnchor(null); void logout(); }}><LogoutIcon fontSize="small" sx={{ mr: 1.5 }} />{loggingOut ? 'Signing out…' : 'Logout'}</MenuItem>
        </Menu>
      </Toolbar>
    </AppBar>
    <Box component="nav" sx={{ width: { md: drawerWidth }, flexShrink: { md: 0 } }}>
      <Drawer variant="temporary" open={mobileOpen} onClose={() => setMobileOpen(false)}
        sx={{ display: { xs: 'block', md: 'none' }, '& .MuiDrawer-paper': { width: drawerWidth, pt: 9 } }}>
        {navigation}
      </Drawer>
      <Drawer variant="permanent" sx={{ display: { xs: 'none', md: 'block' }, '& .MuiDrawer-paper': { width: drawerWidth, boxSizing: 'border-box', borderRight: '1px solid #e2e8f0', pt: 8.4 } }}>
        {navigation}
      </Drawer>
    </Box>
    <Box component="main" id="main-content" sx={{ flex: 1, minWidth: 0, p: { xs: 2, sm: 3, xl: 4 }, pt: { xs: 10, sm: 12, xl: 12 } }}>
      <Box sx={{ maxWidth: 1600, mx: 'auto' }}>
            {notice && (
              <Alert
                severity="success"
                sx={{ mb: 3 }}
                onClose={() => setNotice("")}
              >
                {notice}
              </Alert>
            )}

            {pageError && (
              <Alert
                severity="error"
                sx={{ mb: 3 }}
                onClose={() => setPageError("")}
              >
                {pageError}
              </Alert>
            )}
        <Outlet /></Box>
    </Box>

    <BackupActionDialog
        open={backupOpen}
        busy={busy}
        error={actionError}
        unknownOutcome={unknownOutcome}
        onClose={closeAction}
        onConfirm={() => {
            void confirmAction();
        }}
    />
  </Box>;
}
