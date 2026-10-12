import type { ReactNode } from 'react';
import { Box, Divider, Paper, Typography } from '@mui/material';

type Props = { title: string; subtitle?: string; action?: ReactNode; children: ReactNode };

export default function DashboardCard({ title, subtitle, action, children }: Props) {
  return <Paper component="section" elevation={0} sx={{ border: '1px solid #e2e8f0', overflow: 'hidden', boxShadow: '0 3px 16px rgba(8,41,76,.025)' }}>
    <Box sx={{ p: 2.5, display: 'flex', alignItems: 'flex-start', justifyContent: 'space-between', gap: 2, flexWrap: 'wrap' }}>
      <Box><Typography component="h2" variant="h6" sx={{ fontSize: '1.55rem', fontWeight: 700 }}>{title}</Typography>
        {subtitle && <Typography variant="body2" color="text.secondary" sx={{ mt: .5 }}>{subtitle}</Typography>}
      </Box>
      {action}
    </Box>
    <Divider />
    {children}
  </Paper>;
}
