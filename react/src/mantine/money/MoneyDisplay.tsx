import { Text } from '@mantine/core';
import { useAQLocale } from '../locale';

export interface MoneyDisplayProps {
  amount: number | null | undefined;
  currency?: string | null;
  textProps?: React.ComponentProps<typeof Text>;
}

export function MoneyDisplay({ amount, currency = 'USD', textProps }: MoneyDisplayProps) {
  const { locale } = useAQLocale();

  if (amount == null) {
    return <Text {...textProps}>—</Text>;
  }

  const formatted = new Intl.NumberFormat(locale, {
    minimumFractionDigits: 2,
    maximumFractionDigits: 2,
  }).format(amount);

  return (
    <Text {...textProps}>
      {currency} {formatted}
    </Text>
  );
}
