import { DateInput } from '@mantine/dates';
import type { DateInputProps } from '@mantine/dates';
import { toDateOnlyString, toDateTimeOffsetString } from '../../utils/DateTimeOffsetUtils';
import { toValidDate } from './toValidDate';

const DATE_ONLY = /^(\d{4})-(\d{2})-(\d{2})$/;

function parseValue(value: string | null | undefined): Date | null {
  if (!value) return null;
  const match = DATE_ONLY.exec(value);
  if (match) return new Date(Number(match[1]), Number(match[2]) - 1, Number(match[3]));
  return toValidDate(value);
}

export interface DateOffsetInputProps
  extends Omit<DateInputProps, 'value' | 'defaultValue' | 'onChange'> {
  /** ISO 8601 string with offset, or YYYY-MM-DD */
  value?: string | null;
  /** Called with the picked day, or null when cleared */
  onChange?: (value: string | null) => void;
  /**
   * 'dateTimeOffset' emits local midnight with the user's offset (for DateTimeOffset fields);
   * 'dateOnly' emits YYYY-MM-DD (for DateOnly fields).
   * @default 'dateTimeOffset'
   */
  output?: 'dateTimeOffset' | 'dateOnly';
}

/**
 * Single date input (no time). Never converts through UTC, so the picked day is preserved.
 */
export function DateOffsetInput({
  value,
  onChange,
  output = 'dateTimeOffset',
  clearable = true,
  ...rest
}: DateOffsetInputProps) {
  const handleChange = (picked: unknown) => {
    const date = toValidDate(typeof picked === 'string' && DATE_ONLY.test(picked) ? parseValue(picked) : picked);
    if (!date) {
      onChange?.(null);
      return;
    }
    onChange?.(output === 'dateOnly' ? toDateOnlyString(date) : toDateTimeOffsetString(date));
  };

  return <DateInput {...rest} clearable={clearable} value={parseValue(value)} onChange={handleChange} />;
}
