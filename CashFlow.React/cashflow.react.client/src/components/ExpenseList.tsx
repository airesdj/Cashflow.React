import { Box, Stack, Typography } from '@mui/material';
import type { ExpenseTotal } from '../dashboard/types';
import { categoryColour, money } from '../format';

type Props = { items: ExpenseTotal[]; total: number; currency: string };
export default function ExpenseList({ items, total, currency }: Props) {
  return <Box sx={{ p: 2.5 }}>
    <Stack direction="row" sx={{ justifyContent: 'space-between', alignItems: 'center', mb: 1.5, gap: 2 }}>
      <Typography variant="body2" color="text.secondary" sx={{ fontSize: 22 }}>Total</Typography>
      <Typography sx={{ fontSize: '1.45rem', fontWeight: 700, fontVariantNumeric: 'tabular-nums' }}>{money(total, currency)}</Typography>
    </Stack>
    {items.length === 0 ? <Typography color="text.secondary" sx={{ py: 2 }}>No expenses to display.</Typography> :
      <Box component="ul" sx={{ p: 0, m: 0, listStyle: 'none' }}>
        {items.map((item) => <Box component="li" key={item.oid} sx={{ display: 'flex', alignItems: 'center', gap: 1.5, py: 1.4,
          borderTop: '1px solid #edf1f5' }}>
          <Box aria-hidden sx={{ width: 15, height: 15, borderRadius: '50%', bgcolor: categoryColour(item.oid), flexShrink: 0 }} />
          <Typography variant="body2" sx={{ flex: 1, minWidth: 0, overflowWrap: 'anywhere' }}>{item.name}</Typography>
          <Typography variant="body2" sx={{ fontWeight: 600, whiteSpace: 'nowrap', fontVariantNumeric: 'tabular-nums' }}>{money(item.estimatedValue, currency)}</Typography>
        </Box>)}
      </Box>}
  </Box>;
}
