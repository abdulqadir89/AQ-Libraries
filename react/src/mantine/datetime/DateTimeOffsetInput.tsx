import { DateTimePicker } from '@mantine/dates';
import type { DateTimePickerProps } from '@mantine/dates';
import { toDateTimeOffsetString } from '../../utils/DateTimeOffsetUtils';
import { toValidDate } from './toValidDate';

export interface DateTimeOffsetInputProps
  extends Omit<DateTimePickerProps, 'value' | 'defaultValue' | 'onChange'> {
  /** ISO 8601 string with timezone offset (e.g. 2026-05-06T14:30:00+05:00) */
  value?: string | null;
  /** Called with an ISO 8601 string carrying the user's local offset, or null when cleared */
  onChange?: (value: string | null) => void;
}

/**
 * Single DateTimeOffset input. Takes and returns ISO 8601 strings with the user's
 * local timezone offset (never UTC), so forms need no conversion helpers.
 */
export function DateTimeOffsetInput({ value, onChange, clearable = true, ...rest }: DateTimeOffsetInputProps) {
  return (
    <DateTimePicker
      {...rest}
      clearable={clearable}
      value={toValidDate(value)}
      onChange={(date: unknown) => onChange?.(toDateTimeOffsetString(toValidDate(date)))}
    />
  );
}
