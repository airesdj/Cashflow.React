export function currencyCode(value: string, fallback = 'AUD'): string {
  const code = value.trim().toUpperCase();
  if (!code) return fallback;
  if (code === '$' || code === 'A$' || code === 'AU$') return 'AUD';
  return code;
}

export function money(value: number, currency: string): string {
  const code = currencyCode(currency);
  try { return new Intl.NumberFormat('en-AU', { style: 'currency', currency: code }).format(value); }
  catch { return `${new Intl.NumberFormat('en-AU', { minimumFractionDigits: 2, maximumFractionDigits: 2 }).format(value)} ${code}`; }
}

export function dateLabel(value: string): string {
  // A due date is a calendar date: don't shift it to another day using UTC conversion.
  const [year, month, day] = value.slice(0, 10).split('-');
  return `${day}/${month}/${year}`;
}

export const categoryColours = [
  '#428bca', '#5cb85c', '#f0ad4e', '#ff006e', '#deb0df',
  '#ff55d7', '#5bc0de', '#000080', '#ff00ff', '#800080',
  '#0000ff', '#008080', '#00ffff', '#008000', '#00ff00',
  '#808000', '#ffff00', '#800000', '#ff0000', '#000000',
  '#808080', '#c0c0c0', '#ffbf00', '#ff7f50', '#de3163',
  '#9fe2bf', '#40e0d0', '#6495ed', '#ccccff', '#ffa07a',
];


export function categoryColour(oid: number): string {
  return categoryColours[Math.abs(oid) % categoryColours.length];
}
