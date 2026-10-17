import { Text } from '@mantine/core';
import { formatDate, formatDateOnly } from '../../utils/DateTimeOffsetUtils';

const DATE_ONLY = /^\d{4}-\d{2}-\d{2}$/;

export interface DateDisplayProps {
  /**
   * A date-only value (YYYY-MM-DD). An ISO 8601 string with offset is also accepted and shown as its local day.
   */
  value: string | null | undefined;

  /**
   * Additional props to pass to the Text component
   */
  textProps?: React.ComponentProps<typeof Text>;
}

/**
 * Displays a date-only value (no time). A YYYY-MM-DD value is formatted without going through a timezone,
 * so the day never shifts and no time is shown.
 */
export function DateDisplay({ value, textProps }: DateDisplayProps) {
  const mergedTextProps = { component: 'span' as const, ...textProps };

  const formatted = !value ? '' : DATE_ONLY.test(value) ? formatDate(value) : formatDateOnly(value);

  return <Text {...mergedTextProps}>{formatted || '—'}</Text>;
}
